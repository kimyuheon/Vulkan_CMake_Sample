cmake_minimum_required(VERSION 3.22)
project(vulkancad_android LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 엔진은 **미리 빌드된** libVulkanCADCore.so 를 링크만 한다 — 이 레포엔 엔진 소스가 없다.
#   sdk/lib-android/<ABI>/libVulkanCADCore.so  ← Releases 에서 받아 sdk/ 에 풀거나, 엔진 레포의 build_android.sh 가 복사
# 여기서 컴파일하는 것은 JNI(Kotlin ↔ C API)와 샘플 공용 로직뿐.
get_filename_component(SAMPLE_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../../../../.." ABSOLUTE)   # sample/
get_filename_component(SDK_DIR "${SAMPLE_ROOT}/../sdk" ABSOLUTE)

set(ENGINE_SO "${SDK_DIR}/lib-android/${ANDROID_ABI}/libVulkanCADCore.so")
if(NOT EXISTS "${ENGINE_SO}")
    message(FATAL_ERROR
        "엔진 라이브러리가 없습니다: ${ENGINE_SO}\n"
        "  레포 루트에서 Releases 의 Android SDK 를 받아 sdk/ 에 푸세요:\n"
        "    gh release download <android-sdk 태그> -R kimyuheon/Vulkan_CMake_Sample -p '*.zip'\n"
        "    unzip -o VulkanCAD-Android-SDK-*.zip -d sdk/\n"
        "  (README.md 의 'Android' 절 참고)")
endif()

add_library(VulkanCADCore SHARED IMPORTED)
set_target_properties(VulkanCADCore PROPERTIES IMPORTED_LOCATION "${ENGINE_SO}")

# JNI 라이브러리 — 이름은 그대로 libvulkancad.so (Kotlin System.loadLibrary("vulkancad")).
# 엔진 상태는 엔진 .so 안에 하나 — app·robot 모듈의 JNI 가 같은 엔진을 본다.
add_library(vulkancad SHARED
    "${CMAKE_CURRENT_SOURCE_DIR}/android_jni.cpp"
    "${SAMPLE_ROOT}/android_test/robot/src/main/cpp/robot_jni.cpp"   # 로봇 팔 앱 JNI
    "${SAMPLE_ROOT}/shared/robot_demo.cpp")                          # iOS·macOS 와 공용 로직

target_include_directories(vulkancad PRIVATE
    "${SDK_DIR}/include"          # VulkanCAD_API.h — 공개 C API 하나
    "${SAMPLE_ROOT}/shared")

find_library(log-lib log)
target_link_libraries(vulkancad PRIVATE VulkanCADCore android ${log-lib})
