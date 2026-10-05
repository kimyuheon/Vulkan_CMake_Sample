plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.android")
}

// SDK 위치: local.properties(sdk.dir) > ANDROID_HOME > OS 기본 경로.
fun installedNdkVersion(): String? {
    val localSdk = rootProject.file("local.properties").takeIf { it.exists() }
        ?.readLines()?.firstOrNull { it.startsWith("sdk.dir=") }
        ?.substringAfter("=")?.replace("\\:", ":")?.replace("\\\\", "\\")
    val home = System.getProperty("user.home")
    val sdk = localSdk ?: System.getenv("ANDROID_HOME")
        ?: listOf("$home/Library/Android/sdk", "$home/AppData/Local/Android/Sdk", "$home/Android/Sdk")
            .firstOrNull { file(it).exists() }
        ?: return null
    return file("$sdk/ndk").listFiles { f -> f.isDirectory }
        ?.map { it.name }
        ?.maxWithOrNull(compareBy<String>(
            { it.split(".").getOrNull(0)?.toIntOrNull() ?: 0 },
            { it.split(".").getOrNull(1)?.toIntOrNull() ?: 0 },
            { it.split(".").getOrNull(2)?.toIntOrNull() ?: 0 }))
}

// ── 엔진 모듈 ──
// 엔진 C++ 전체를 libvulkancad.so 로 빌드하고, 렌더 뷰(VulkanSurfaceView)·JNI(CadNative)·
// 런타임 에셋(models/fonts/textures)을 담는다. 앱 모듈(app, robot …)은 이것만 참조하면 된다.
// ⚠️ Kotlin 패키지는 com.vulkancad.androidtest 그대로 — JNI 함수 이름
//    (Java_com_vulkancad_androidtest_CadNative_*)이 이 패키지에 묶여 있다.
//    namespace 만 다르게 둔다(앱 모듈과 같으면 R 클래스가 겹친다).
android {
    namespace = "com.vulkancad.engine"
    compileSdk = 34
    // 머신마다 설치된 NDK 버전이 달라도 되도록 <sdk>/ndk/ 중 가장 최신을 자동 선택.
    // 하나도 없으면 AGP 기본 버전(필요 시 자동 다운로드).
    installedNdkVersion()?.let { ndkVersion = it }

    defaultConfig {
        minSdk = 28          // posix_spawn (AI 런처) 은 API 28+
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
        debug {
            isJniDebuggable = true
            // 엔진은 미리 빌드된 Release .so(sdk/lib-android) — 디버그 APK 여도 엔진은 최적화돼 있다.
            // 여기서 Debug 로 컴파일되는 것은 JNI·샘플 로직뿐이라 속도에 영향이 없다.
        }
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
