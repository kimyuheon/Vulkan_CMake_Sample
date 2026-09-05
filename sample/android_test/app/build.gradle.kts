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

    // 런타임 에셋(모델/폰트/텍스처)을 assets 로 포함.
    sourceSets["main"].assets.srcDir(layout.buildDirectory.dir("engineAssets"))
}

// 런타임 에셋을 APK assets 로 복사. 레포의 sdk/ 를 먼저 쓰고, 없으면 엔진 데스크톱
// 빌드 출력을 찾는다 (Windows VS=build/Debug, macOS/Linux Ninja/Make=build).
// 셰이더는 라이브러리에 SPIR-V 로 내장돼 있어 복사할 것이 없다.
val copyEngineAssets by tasks.registering(Copy::class) {
    val engRoot = file("${rootDir}/../..")
    val eng = listOf("sdk", "build/Debug", "build", "cmake-build-debug", "build-mac/Debug")
        .map { engRoot.resolve(it) }
        .firstOrNull { it.resolve("fonts").exists() }
        ?: engRoot.resolve("sdk")
    doFirst {
        if (!eng.resolve("fonts").exists())
            logger.warn("런타임 에셋(fonts)을 못 찾음: $eng — sdk/ 를 확인하세요")
    }
    into(layout.buildDirectory.dir("engineAssets"))
    from("${eng}/models")   { into("models") }
    from("${eng}/fonts")    { into("fonts") }
    from("${eng}/textures") { into("textures") }
}
tasks.named("preBuild") { dependsOn(copyEngineAssets) }

dependencies {
    implementation("androidx.core:core-ktx:1.13.1")
}
