cmake_minimum_required(VERSION 3.21)
project(VulkanCadQt LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_AUTOMOC ON)

# ── Qt6 (Quick/Gui/Widgets) ──
# Widgets 는 QFileDialog(파일 열기/저장) 용 — qt6-base 에 이미 들어 있어 런타임 패키지가 늘지 않는다.
find_package(Qt6 REQUIRED COMPONENTS Quick Gui Widgets)
qt_standard_project_setup(REQUIRES 6.4)

# ── 엔진 공유 라이브러리 위치 ──
# 엔진을 먼저 빌드해야 함:  (레포 루트에서)
#   cmake -B build && cmake --build build --target VulkanCADCoreShared
# → build/libVulkanCADCore.so 생성. 다른 경로면 -DVULKANCAD_ENGINE_BUILD=/path 로 지정.
set(VULKANCAD_ENGINE_BUILD "${CMAKE_CURRENT_SOURCE_DIR}/../../sdk"
    CACHE PATH "VulkanCAD 엔진 빌드 폴더")

# Visual Studio 같은 multi-config 생성기는 DLL/import library를 Debug/Release 아래에 둔다.
set(_vulkancad_search_paths
    "${VULKANCAD_ENGINE_BUILD}"
    "${VULKANCAD_ENGINE_BUILD}/Debug"
    "${VULKANCAD_ENGINE_BUILD}/Release"
    "${VULKANCAD_ENGINE_BUILD}/RelWithDebInfo"
    "${VULKANCAD_ENGINE_BUILD}/MinSizeRel")

find_library(VULKANCAD_CORE_LIB
    NAMES VulkanCADCore
    PATHS ${_vulkancad_search_paths}
    NO_DEFAULT_PATH)
if(NOT VULKANCAD_CORE_LIB)
    message(FATAL_ERROR
        "VulkanCADCore 공유 라이브러리를 찾지 못했습니다: ${VULKANCAD_ENGINE_BUILD}\n"
        "먼저 엔진을 빌드하세요:  cmake --build build --target VulkanCADCoreShared")
endif()
get_filename_component(VULKANCAD_CORE_DIR "${VULKANCAD_CORE_LIB}" DIRECTORY)

# 실행파일 타겟 이름은 QML 모듈 URI(VulkanCadQml) 와 달라야 함 — 같으면 모듈 출력 디렉토리와
# 실행파일 경로가 충돌한다("cannot open output file: is a directory").
qt_add_executable(qml_host
    main.cpp
    vulkan_viewport.cpp
    vulkan_viewport.h
    cad_commands.h
)

# QML 모듈 — URI 는 main.cpp 의 리소스 경로 / Main.qml 의 import 와 일치해야 함.
# QML_ELEMENT/QML_SINGLETON 이 붙은 C++ 타입(VulkanViewport, CadCommands)도 자동 등록.
qt_add_qml_module(qml_host
    URI VulkanCadQml
    VERSION 1.0
    QML_FILES Main.qml
)

target_link_libraries(qml_host PRIVATE
    Qt6::Quick
    Qt6::Gui
    Qt6::Widgets
    ${VULKANCAD_CORE_LIB}
)

# Qt Creator에서 바로 실행해도 엔진 에셋 폴더를 알 수 있게 기본 경로를 컴파일한다.
file(TO_CMAKE_PATH "${VULKANCAD_CORE_DIR}" VULKANCAD_ASSET_PATH_CMAKE)
target_compile_definitions(qml_host PRIVATE
    VULKANCAD_DEFAULT_ASSET_PATH="${VULKANCAD_ASSET_PATH_CMAKE}")

# Linux/macOS는 rpath로 공유 라이브러리를 찾는다. LD_LIBRARY_PATH가 필요 없다.
set_target_properties(qml_host PROPERTIES
    BUILD_RPATH "${VULKANCAD_CORE_DIR}"
    INSTALL_RPATH "${VULKANCAD_CORE_DIR}")

# Windows는 import library와 별도로 실제 DLL을 실행파일 옆에 복사해야 한다.
if(WIN32)
    find_file(VULKANCAD_CORE_DLL
        NAMES VulkanCADCore.dll
        PATHS ${_vulkancad_search_paths}
        NO_DEFAULT_PATH)
    if(NOT VULKANCAD_CORE_DLL)
        message(FATAL_ERROR "VulkanCADCore.dll을 찾지 못했습니다: ${VULKANCAD_ENGINE_BUILD}")
    endif()
    add_custom_command(TARGET qml_host POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
                "${VULKANCAD_CORE_DLL}" "$<TARGET_FILE_DIR:qml_host>"
        COMMENT "Copying VulkanCADCore.dll next to Qt sample")
endif()
