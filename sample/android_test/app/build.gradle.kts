plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

// 기본 테스트 앱 — 그리기·치수·파일 열기. 엔진·렌더 뷰·에셋은 :engine 모듈에서 온다.
android {
    namespace = "com.lotcad.androidtest"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.lotcad.androidtest"
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
    implementation("androidx.core:core-ktx:1.13.1")
}
