plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

// 로봇 팔 샘플 — URDF 로봇을 관절 슬라이더로 움직인다. 로직은 sample/shared/robot_demo (iOS 와 공용).
// 엔진·렌더 뷰·에셋은 :engine 모듈에서 온다. 로봇 JNI(robot_jni.cpp)는 엔진 .so 에 같이 빌드된다
// (:engine 의 CMakeLists.txt 참고 — .so 를 하나로 두어야 엔진 전역 상태를 공유한다).
android {
    namespace = "com.vulkancad.robotarm"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.vulkancad.robotarm"
        minSdk = 28
        targetSdk = 34
        versionCode = 1
        versionName = "1.0"
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }
}

dependencies {
    implementation(project(":engine"))
}
