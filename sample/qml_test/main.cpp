// VulkanCAD — Qt6 QML 호스트 샘플 (Windows / Ubuntu X11 / macOS)
//
// QML 로 툴바/패널 UI 를 만들고, 3D 뷰 영역엔 엔진의 Vulkan 렌더를 네이티브 자식 창으로 얹는다.
// WPF/MFC/iOS 호스트와 같은 C API 임베드 패턴 — 플랫폼 창/입력 코드만 Qt 로 다를 뿐.
//
// 런타임 에셋(models, fonts, textures)은 레포의 sdk/ 에 있으므로,
// CMake 가 그 경로를 컴파일에 박아 둔다. 다른 위치의 에셋을 쓰려면:
//   - 환경변수 VULKANCAD_ASSETS=/path/to/sdk
//   - 또는 첫 인자로 경로 전달:  ./VulkanCadQml /path/to/sdk

#include "../../sdk/include/VulkanCAD_API.h"

#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include <QCoreApplication>
#include <QDir>
#include <QFileInfo>
#include <QString>
#include <QtGlobal>
#include <cstdlib>

int main(int argc, char* argv[]) {
#if defined(Q_OS_LINUX)
    // 엔진의 Linux 임베드 backend(X11NativeViewWindow)는 xlib(X11) surface 전용이다.
    // QQuickItem 의 winId() 가 반드시 "X11 XID" 여야 하므로 Qt 를 xcb 로 **무조건** 강제한다.
    // Wayland 세션에서도 XWayland 로 뜬다.
    //
    // ⚠️ "비어있을 때만" 설정하면 안 된다: Wayland 세션/런처(Qt Creator 등)가
    //    QT_QPA_PLATFORM=wayland 를 이미 심어두면 그 조건을 건너뛰어 Qt 가 Wayland 로 뜨고,
    //    winId() 가 X11 XID 가 아닌 Wayland 핸들이 되어 xlib surface 가 깨진다
    //    ("surface is not supported by the physicalDevice" → swapchain acquire 실패).
    qputenv("QT_QPA_PLATFORM", "xcb");
#endif

    // Vulkan SDK를 쓰는 개발 환경에서는 validation layer/ICD 경로를 자동 설정한다.
    // 이미 사용자가 지정한 값은 덮어쓰지 않는다.
    const QByteArray vulkanSdk = qgetenv("VULKAN_SDK");
    if (!vulkanSdk.isEmpty() && qEnvironmentVariableIsEmpty("VK_LAYER_PATH")) {
        const QString layerPath = QDir(QString::fromLocal8Bit(vulkanSdk))
            .filePath("share/vulkan/explicit_layer.d");
        if (QFileInfo(layerPath).isDir())   // isDir 은 static 아님 — 인스턴스로 호출
            qputenv("VK_LAYER_PATH", QDir::toNativeSeparators(layerPath).toLocal8Bit());
    }
#if defined(Q_OS_MACOS)
    if (!vulkanSdk.isEmpty() && qEnvironmentVariableIsEmpty("VK_ICD_FILENAMES")) {
        const QString icdPath = QDir(QString::fromLocal8Bit(vulkanSdk))
            .filePath("share/vulkan/icd.d/MoltenVK_icd.json");
        if (QFileInfo::exists(icdPath))
            qputenv("VK_ICD_FILENAMES", QDir::toNativeSeparators(icdPath).toLocal8Bit());
    }
#endif

    QGuiApplication app(argc, argv);

    // ── 엔진 런타임 에셋 경로 지정 (CAD_CreateEngine 전에 호출해야 반영됨) ──
    QString assets;
    if (argc > 1) {
        assets = QString::fromLocal8Bit(argv[1]);
    } else if (const char* env = std::getenv("VULKANCAD_ASSETS")) {
        assets = QString::fromLocal8Bit(env);
    } else {
#ifdef VULKANCAD_DEFAULT_ASSET_PATH
        // CMake가 엔진 build 경로를 심어주므로 Qt Creator 실행에도 별도 shell 설정이 필요 없다.
        assets = QString::fromUtf8(VULKANCAD_DEFAULT_ASSET_PATH);
#else
        assets = QDir(QCoreApplication::applicationDirPath()).filePath("../../../build");
#endif
    }
    if (!assets.isEmpty()) {
        const QByteArray a = QDir(assets).absolutePath().toLocal8Bit();
        if (!CAD_SetRuntimeAssetPath(a.constData())) {
            qWarning("[qml] runtime asset path 설정 실패: %s\n"
                     "      모델/폰트를 못 찾으면 치수·문자 생성이 실패합니다.\n"
                     "      VULKANCAD_ASSETS 로 엔진 build 폴더를 지정하세요.", a.constData());
        }
    }

    QQmlApplicationEngine engine;
    // Qt 6.4 엔 loadFromModule(6.5+) 이 없어 qt_add_qml_module 이 심는 리소스 URL 로 로드.
    // Qt 6.4 기본 리소스 접두사: qrc:/<URI>/Main.qml  (6.5+ 의 qrc:/qt/qml/... 와 다름)
    engine.load(QUrl(QStringLiteral("qrc:/VulkanCadQml/Main.qml")));
    if (engine.rootObjects().isEmpty()) {
        return -1;
    }

    return app.exec();
}
