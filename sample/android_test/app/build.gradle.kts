plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.vulkancad.androidtest"
    compileSdk = 34
    // 머신에 설치된 NDK 버전으로 (SDK Manager 에서 확인). 다르면 여기만 수정.
    ndkVersion = "30.0.15729638"

    defaultConfig {
        applicationId = "com.vulkancad.androidtest"
        minSdk = 28          // posix_spawn (AI 런처) 은 API 28+
        targetSdk = 34
        versionCode = 1
        versionName = "1.0"
        // x86_64 = 인텔맥/윈도 에뮬레이터, arm64-v8a = 애플실리콘 에뮬레이터/실기.
        // 둘 다 빌드 → 어느 호스트/기기든 동작 (빌드 시간은 배로).
        ndk { abiFilters += listOf("x86_64", "arm64-v8a") }
        externalNativeBuild {
            cmake { arguments += "-DANDROID_STL=c++_static" }
        }
    }

    externalNativeBuild {
        cmake {
            path = file("src/main/cpp/CMakeLists.txt")
        }
    }

    buildTypes {
        debug { isJniDebuggable = true }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }

    // 엔진 에셋(데스크톱 빌드의 .spv/모델/폰트)을 assets 로 포함.
    sourceSets["main"].assets.srcDir(layout.buildDirectory.dir("engineAssets"))
}

// 데스크톱 빌드 산출물의 런타임 에셋을 APK assets 로 복사.
// (.spv 는 SPIR-V 라 플랫폼 독립 → 데스크톱에서 컴파일된 것 재사용)
// 데스크톱 빌드 출력 위치는 OS/생성기별로 달라 후보를 탐색 (Windows VS=build/Debug,
// macOS/Linux Ninja/Make=build). shaders 폴더가 있는 첫 후보를 사용.
val copyEngineAssets by tasks.registering(Copy::class) {
    val engRoot = file("${rootDir}/../..")
    val eng = listOf("build/Debug", "build", "cmake-build-debug", "build-mac/Debug")
        .map { engRoot.resolve(it) }
        .firstOrNull { it.resolve("shaders").exists() }
        ?: engRoot.resolve("build/Debug")
    doFirst {
        if (!eng.resolve("shaders").exists())
            logger.warn("엔진 에셋(shaders)을 못 찾음: $eng — 데스크톱을 먼저 빌드하세요")
    }
    into(layout.buildDirectory.dir("engineAssets"))
    from("${eng}/shaders")  { into("shaders") }
    from("${eng}/models")   { into("models") }
    from("${eng}/fonts")    { into("fonts") }
    from("${eng}/textures") { into("textures") }
}
tasks.named("preBuild") { dependsOn(copyEngineAssets) }

dependencies {
    implementation("androidx.core:core-ktx:1.13.1")
}
