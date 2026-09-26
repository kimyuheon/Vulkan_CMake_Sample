cmake_minimum_required(VERSION 3.22)
project(vulkancad_android LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 엔진 레포 루트: samples/android_test/app/src/main/cpp 에서 6단계 위.
get_filename_component(ENGINE_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../../../../../.." ABSOLUTE)
# 샘플 레포(3dEngine_Sample)엔 엔진 소스가 없다 → 옆 폴더 3dEngine 을 사용.
if(NOT EXISTS "${ENGINE_ROOT}/first_app.cpp" AND EXISTS "${ENGINE_ROOT}/../3dEngine/first_app.cpp")
    get_filename_component(ENGINE_ROOT "${ENGINE_ROOT}/../3dEngine" ABSOLUTE)
endif()

# ── 호스트 OS 별 헤더 경로 (glm + GLFW). vulkan 헤더는 NDK sysroot 가 제공. ──
# (이 CMake 는 NDK 툴체인으로 돌지만 CMAKE_HOST_* 는 빌드하는 PC 의 OS)
set(VK_SDK_ROOT "${ENGINE_ROOT}/../VulkanSdk")
if(CMAKE_HOST_WIN32)
    # Windows: 검증된 경로 (Include 에 glm/vulkan/stb, glfw 바이너리 배포에 헤더)
    set(VK_INCLUDE "${VK_SDK_ROOT}/Win/Include")
    file(GLOB GLFW_INCLUDE_DIRS "${VK_SDK_ROOT}/Win/glfw*/include")
else()
    # macOS / Linux: glm 과 GLFW 헤더를 SDK 또는 brew/system 에서 탐색.
    if(CMAKE_HOST_APPLE)
        set(_vk_plat "Apple")
    else()
        set(_vk_plat "Linux")
    endif()
    # ⚠️ Android NDK 툴체인은 CMAKE_FIND_ROOT_PATH_MODE_INCLUDE=ONLY 라
    #    find_path 가 NDK sysroot 안만 뒤진다 → 호스트 헤더는 NO_CMAKE_FIND_ROOT_PATH 필요.
    find_path(GLM_INCLUDE_DIR glm/glm.hpp
        PATHS "${VK_SDK_ROOT}/${_vk_plat}/Include" "${VK_SDK_ROOT}/${_vk_plat}/include"
              /opt/homebrew/include /usr/local/include /usr/include
        NO_CMAKE_FIND_ROOT_PATH)
    # GLFW 헤더는 SDK 의 GLFW/include 하위 (Apple 배치). 그 외 brew/system 폴백.
    find_path(GLFW_INCLUDE_DIR GLFW/glfw3.h
        PATHS "${VK_SDK_ROOT}/${_vk_plat}/GLFW/include"
              "${VK_SDK_ROOT}/${_vk_plat}/Include" "${VK_SDK_ROOT}/${_vk_plat}/include"
              /opt/homebrew/include /usr/local/include /usr/include
        NO_CMAKE_FIND_ROOT_PATH)
    set(VK_INCLUDE "${GLM_INCLUDE_DIR}")
    set(GLFW_INCLUDE_DIRS "${GLFW_INCLUDE_DIR}")
endif()

message(STATUS "ENGINE_ROOT       = ${ENGINE_ROOT}")
message(STATUS "VK_INCLUDE(glm)   = ${VK_INCLUDE}")
message(STATUS "GLFW_INCLUDE_DIRS = ${GLFW_INCLUDE_DIRS}")

# ── 엔진 소스 수집 ──
file(GLOB ENGINE_SRC   "${ENGINE_ROOT}/*.cpp")
file(GLOB API_SRC      "${ENGINE_ROOT}/api/*.cpp")
file(GLOB BOOLEAN_SRC  "${ENGINE_ROOT}/boolean/*.cpp")
file(GLOB DIMENSION_SRC "${ENGINE_ROOT}/dimension/*.cpp")
file(GLOB MODIFY_SRC   "${ENGINE_ROOT}/modify/*.cpp")
# FirstApp 은 first_app/ 하위로 분할돼 있다 — 빠뜨리면 FirstApp::* 전부 undefined.
file(GLOB FIRSTAPP_SRC "${ENGINE_ROOT}/first_app/*.cpp")
file(GLOB IMAGE_SRC    "${ENGINE_ROOT}/image/*.cpp")
# 데스크톱 CMakeLists 와 같은 하위 폴더들 — 새 폴더가 생기면 여기에도 추가.
file(GLOB PLUGIN_SRC   "${ENGINE_ROOT}/plugin/*.cpp")
file(GLOB UI_SRC       "${ENGINE_ROOT}/ui/*.cpp")
file(GLOB VOLUME_SRC   "${ENGINE_ROOT}/volume/*.cpp")
file(GLOB MEDICAL_SRC  "${ENGINE_ROOT}/medical/*.cpp")
file(GLOB DXF_SRC      "${ENGINE_ROOT}/dxf/*.cpp")
file(GLOB GEOMETRY_SRC "${ENGINE_ROOT}/geometry/*.cpp")

# ufbx / miniz — 데스크톱처럼 C 파일을 C++ 로 컴파일.
set(UFBX_C  "${ENGINE_ROOT}/external/ufbx/ufbx.c")
set(MINIZ_C "${ENGINE_ROOT}/external/miniz/miniz.c")
set_source_files_properties("${UFBX_C}" PROPERTIES LANGUAGE CXX)
set_source_files_properties("${MINIZ_C}" PROPERTIES LANGUAGE CXX
    COMPILE_DEFINITIONS "MINIZ_NO_ARCHIVE_APIS;MINIZ_NO_STDIO;MINIZ_NO_DEFLATE_APIS")

# 셰이더 내장 — 엔진 shaders/*.spv 를 바이트 배열 cpp 로 묶는다 (데스크톱과 같은 스크립트).
set(EMBEDDED_SHADERS_CPP "${CMAKE_CURRENT_BINARY_DIR}/generated/lot_embedded_shaders_data.cpp")
file(GLOB SPV_FILES "${ENGINE_ROOT}/shaders/*.spv")
if(NOT SPV_FILES)
    # .spv 는 git 에 없다 — 데스크톱 빌드가 glslangValidator 로 만든다.
    message(FATAL_ERROR "셰이더(.spv) 없음: ${ENGINE_ROOT}/shaders — 엔진 데스크톱 빌드(./build.sh)를 먼저 한 번 돌리세요.")
endif()
add_custom_command(
    OUTPUT "${EMBEDDED_SHADERS_CPP}"
    COMMAND ${CMAKE_COMMAND} -E make_directory "${CMAKE_CURRENT_BINARY_DIR}/generated"
    COMMAND ${CMAKE_COMMAND}
        -DSPV_DIR=${ENGINE_ROOT}/shaders
        -DOUT_CPP=${EMBEDDED_SHADERS_CPP}
        -P "${ENGINE_ROOT}/cmake/embed_shaders.cmake"
    DEPENDS ${SPV_FILES} "${ENGINE_ROOT}/cmake/embed_shaders.cmake"
    VERBATIM)

# 데스크톱/타 플랫폼 전용 제외 (iOS 제외 목록 미러).
list(FILTER ENGINE_SRC EXCLUDE REGEX "/main\\.cpp$")
# GLFW / ImGui / 데스크톱 UI 클래스 — Android 미사용 (ios_stubs.cpp 가 빈 구현 제공).
list(FILTER ENGINE_SRC EXCLUDE REGEX "/(lot_window|lot_file_dialog|imgui_glfw_bridge|lot_main_menu|lot_status_bar|lot_ui_manager|lot_toolbar|lot_properties_panel|ui_input_capture)\\.cpp$")
# AI 창은 OS별 — win/linux/apple 전부 제외 (LotAIWindow::render 는 ios_stubs 가 스텁).
list(FILTER ENGINE_SRC EXCLUDE REGEX "/lot_ai_window_(win|linux|apple)\\.cpp$")
# 타 플랫폼 네이티브뷰 — Android 는 android_native_view_window.cpp (GLOB 에 이미 포함, __ANDROID__ 가드).
list(FILTER ENGINE_SRC EXCLUDE REGEX "/(macos_native_view_window|win32_native_view_window)\\.cpp$")
# ios_stubs.cpp 는 시그니처 드리프트로 재사용 불가 → 제외하고 android_stubs.cpp 로 대체.
list(FILTER ENGINE_SRC EXCLUDE REGEX "/ios_stubs\\.cpp$")

set(ALL_SRC
    ${ENGINE_SRC} ${API_SRC} ${BOOLEAN_SRC} ${DIMENSION_SRC} ${MODIFY_SRC}
    ${FIRSTAPP_SRC} ${IMAGE_SRC}
    ${PLUGIN_SRC} ${UI_SRC} ${VOLUME_SRC} ${MEDICAL_SRC} ${DXF_SRC} ${GEOMETRY_SRC}
    "${UFBX_C}" "${MINIZ_C}" "${EMBEDDED_SHADERS_CPP}"
    "${CMAKE_CURRENT_SOURCE_DIR}/android_jni.cpp"
    "${CMAKE_CURRENT_SOURCE_DIR}/android_stubs.cpp"
)

# ── Manifold (CSG) ──
set(MANIFOLD_PAR OFF CACHE BOOL "" FORCE)
set(MANIFOLD_CBIND OFF CACHE BOOL "" FORCE)
set(MANIFOLD_PYBIND OFF CACHE BOOL "" FORCE)
set(MANIFOLD_CROSS_SECTION OFF CACHE BOOL "" FORCE)
set(MANIFOLD_EXPORT OFF CACHE BOOL "" FORCE)
set(MANIFOLD_TEST OFF CACHE BOOL "" FORCE)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
add_subdirectory("${ENGINE_ROOT}/external/manifold" manifold_build EXCLUDE_FROM_ALL)

add_library(vulkancad SHARED ${ALL_SRC})

target_compile_definitions(vulkancad PRIVATE
    VULKANCAD_API_EXPORTS
    LOT_NO_IMGUI
    GLFW_INCLUDE_NONE
    VK_USE_PLATFORM_ANDROID_KHR
    # 엔진의 "모바일 네이티브뷰 모드" 가드 재사용 (GLFW 윈도우/데스크톱 다이얼로그 비활성).
    # iOS 전용(Metal)은 .mm/TARGET_OS_IOS 라 Android(__ANDROID__)엔 영향 없음.
    LOT_PLATFORM_IOS
)

target_include_directories(vulkancad PRIVATE
    ${ENGINE_ROOT}
    "${ENGINE_ROOT}/external"      # stb/ tinyobjloader/ tinygltf/ 헤더
    ${VK_INCLUDE}                  # glm (+ Windows 는 vulkan/stb 도)
    ${GLFW_INCLUDE_DIRS}           # GLFW_KEY_* 매크로/타입만 (링크 X, 네이티브뷰)
    "${ENGINE_ROOT}/AI_external/cpp-httplib"
    "${ENGINE_ROOT}/AI_external/json"
)

# vulkan / android / log 는 NDK sysroot 제공.
find_library(log-lib log)
target_link_libraries(vulkancad PRIVATE manifold vulkan android ${log-lib})
