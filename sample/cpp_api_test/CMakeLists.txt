# LotCAD — C++ 콘솔 최소 예제
#
# 엔진 소스 없이 sdk/ 만으로 빌드한다. 필요한 것은 헤더 1개 + 공유 라이브러리뿐이다.
#
#   cmake -B build
#   cmake --build build
#
# SDK 가 다른 곳에 있으면:  cmake -B build -DLOTCAD_SDK=/경로

cmake_minimum_required(VERSION 3.16)
project(LotCADCppApiTest LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(LOTCAD_SDK "${CMAKE_CURRENT_SOURCE_DIR}/../../sdk"
    CACHE PATH "LotCAD SDK 폴더 (라이브러리 + include/ + 런타임 에셋)")

if(NOT EXISTS "${LOTCAD_SDK}/include/LotCAD_API.h")
    message(FATAL_ERROR
        "SDK 를 찾지 못했습니다: ${LOTCAD_SDK}\n"
        "  -DLOTCAD_SDK=<경로> 로 지정하거나, 레포 루트의 sdk/ 를 확인하세요.")
endif()

# Visual Studio 같은 multi-config 생성기는 산출물을 Debug/Release 아래에 둔다.
find_library(LOTCAD_CORE_LIB
    NAMES LotCADCore
    PATHS "${LOTCAD_SDK}"
          "${LOTCAD_SDK}/Debug" "${LOTCAD_SDK}/Release"
          "${LOTCAD_SDK}/RelWithDebInfo" "${LOTCAD_SDK}/MinSizeRel"
    NO_DEFAULT_PATH)
if(NOT LOTCAD_CORE_LIB)
    message(FATAL_ERROR "LotCADCore 라이브러리를 찾지 못했습니다: ${LOTCAD_SDK}")
endif()
get_filename_component(LOTCAD_LIB_DIR "${LOTCAD_CORE_LIB}" DIRECTORY)

add_executable(cpp_api_test main.cpp)
target_include_directories(cpp_api_test PRIVATE "${LOTCAD_SDK}/include")
target_link_libraries(cpp_api_test PRIVATE "${LOTCAD_CORE_LIB}")

if(WIN32)
    # 창을 만들려면 필요 (CAD_AttachView 에 HWND 를 넘긴다).
    target_link_libraries(cpp_api_test PRIVATE user32 gdi32)
    # Windows 는 rpath 가 없다 — DLL 이 exe 옆에 있어야 실행된다.
    add_custom_command(TARGET cpp_api_test POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${LOTCAD_LIB_DIR}/LotCADCore.dll" "$<TARGET_FILE_DIR:cpp_api_test>"
        VERBATIM)
    # 런타임 에셋도 실행 위치 옆으로. (셰이더는 라이브러리에 내장돼 있어 필요 없다)
    foreach(_a models textures fonts)
        add_custom_command(TARGET cpp_api_test POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E copy_directory
                "${LOTCAD_SDK}/${_a}" "$<TARGET_FILE_DIR:cpp_api_test>/${_a}"
            VERBATIM)
    endforeach()
else()
    # macOS/Linux 는 rpath 로 SDK 를 직접 가리킨다 — 복사가 필요 없다.
    set_target_properties(cpp_api_test PROPERTIES
        BUILD_RPATH   "${LOTCAD_LIB_DIR}"
        INSTALL_RPATH "${LOTCAD_LIB_DIR}")
    # 엔진은 상대 경로로 에셋을 연다. 실행 디렉토리에 없으면 SDK 를 쓰도록 알려 준다.
    target_compile_definitions(cpp_api_test PRIVATE
        LOTCAD_DEFAULT_ASSET_PATH="${LOTCAD_SDK}")
endif()

message(STATUS "LotCAD SDK : ${LOTCAD_SDK}")
message(STATUS "  library     : ${LOTCAD_CORE_LIB}")
