# VulkanCAD 샘플 — 최상위 CMake.
#
# 이걸 여는 목적은 하나다: **IDE 로 레포째 열기.** Qt Creator 는 CMakeLists.txt 를
# 그대로 프로젝트로 읽으므로, 이 파일 하나면 샘플 전체가 한 프로젝트로 열린다.
#
#   Qt Creator → 파일/프로젝트 열기 → 3dEngine_Sample/CMakeLists.txt
#   cmake -B build && cmake --build build          (명령행도 같다)
#
# 샘플 하나만 열고 싶으면 그 폴더의 CMakeLists.txt 를 열어도 된다 — 각자 독립이다.
#
# CMake 로 짓는 것만 모은다. MFC(vcxproj) · WPF(dotnet) · Swift(SwiftPM/Xcode) ·
# Android(Gradle) 는 그 생태계의 도구가 자연스러워서 여기 넣지 않는다.

cmake_minimum_required(VERSION 3.21)
project(VulkanCADSamples LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_subdirectory(sample/cpp_api_test)

# qml_test 는 Qt 가 있을 때만. 없다고 나머지까지 실패하면 안 된다.
find_package(Qt6 QUIET COMPONENTS Quick Gui Widgets)
if(Qt6_FOUND)
    add_subdirectory(sample/qml_test)
else()
    message(STATUS "qml_test: Qt6 를 못 찾아 건너뜁니다 (-DCMAKE_PREFIX_PATH=<Qt 경로>)")
endif()

# ⚠️ 실행할 때 **작업 디렉터리를 sdk/ 로** 잡아야 한다.
#    엔진은 에셋도 plugins/ 도 상대 경로로 연다 — 기준은 실행 파일 위치가 아니라
#    현재 작업 디렉터리다. (호스트가 CAD_SetRuntimeAssetPath 를 부르면 그게 chdir 이라
#    그 뒤로는 그 폴더가 기준이 된다. qml_test 가 그렇게 한다.)
message(STATUS "실행 시 작업 디렉터리: ${CMAKE_CURRENT_SOURCE_DIR}/sdk")
message(STATUS "  플러그인은 여기에 둔다: ${CMAKE_CURRENT_SOURCE_DIR}/sdk/plugins")
