// JNI 브릿지 — Kotlin CadNative(object) 의 external fun ↔ C API(CAD_*).
// 패키지: com.vulkancad.androidtest  → Java_com_vulkancad_androidtest_CadNative_<method>

#include <jni.h>
#include <android/native_window.h>
#include <android/native_window_jni.h>
#include <android/log.h>

#include "VulkanCAD_API.h"

#include <string>   // 클립보드 보관
#include <cstdio>
#include <thread>
#include <unistd.h>  // pipe/dup2 — 엔진 stdout 을 logcat 으로

#define LOGI(...) __android_log_print(ANDROID_LOG_INFO,  "VulkanCAD", __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, "VulkanCAD", __VA_ARGS__)

namespace {
    ANativeWindow* g_window = nullptr;

    // 엔진 로그(std::cout/cerr) → logcat.
    // 안드로이드 앱의 stdout/stderr 는 /dev/null 이라 엔진이 찍는 실패 이유("[Model] …", "[Dim] …")가
    // 통째로 사라졌다 — "Fox.glb 가 안 열린다" 를 봐도 원인을 알 길이 없었다. 파이프로 가로채
    // 줄 단위로 "VulkanCAD-stdout" 태그에 흘린다. 확인: adb logcat -s VulkanCAD-stdout
    void startStdoutToLogcat() {
        static bool started = false;
        if (started) return;
        started = true;
        setvbuf(stdout, nullptr, _IOLBF, 0);   // 줄 단위로 바로 흘러가게
        setvbuf(stderr, nullptr, _IONBF, 0);
        static int fds[2];
        if (pipe(fds) != 0) return;
        dup2(fds[1], STDOUT_FILENO);
        dup2(fds[1], STDERR_FILENO);
        std::thread([] {
            char buf[1024];
            std::string line;
            ssize_t n;
            while ((n = read(fds[0], buf, sizeof(buf))) > 0) {
                line.append(buf, static_cast<size_t>(n));
                size_t pos;
                while ((pos = line.find('\n')) != std::string::npos) {
                    __android_log_print(ANDROID_LOG_INFO, "VulkanCAD-stdout", "%s", line.substr(0, pos).c_str());
                    line.erase(0, pos + 1);
                }
            }
        }).detach();
    }
}

extern "C" {

JNIEXPORT jint JNI_OnLoad(JavaVM*, void*) {
    startStdoutToLogcat();   // 라이브러리가 올라오자마자 — 엔진 생성 로그부터 잡히게
    return JNI_VERSION_1_6;
}

#define JNI(ret, name) JNIEXPORT ret JNICALL Java_com_vulkancad_androidtest_CadNative_##name

JNI(void, nativeSetAssetPath)(JNIEnv* env, jobject, jstring path) {
    const char* c = env->GetStringUTFChars(path, nullptr);
    CAD_SetRuntimeAssetPath(c);
    env->ReleaseStringUTFChars(path, c);
}

JNI(jboolean, nativeSurfaceCreated)(JNIEnv* env, jobject, jobject surface, jint w, jint h) {
    if (g_window) { ANativeWindow_release(g_window); g_window = nullptr; }
    g_window = ANativeWindow_fromSurface(env, surface);
    if (!g_window) { LOGE("ANativeWindow_fromSurface failed"); return JNI_FALSE; }
    // 계약: AttachView(핸들) 등록 → CreateEngine 이 그 ANativeWindow 에 렌더.
    // 엔진이 이미 살아 있으면(창만 뗐다 돌아온 경우) AttachView 가 새 창에 다시 붙이고
    // CreateEngine 은 아무것도 안 하고 true — 장면이 그대로 이어진다.
    CAD_AttachView(g_window, w, h);
    const bool ok = CAD_CreateEngine();
    // 손가락/가상 커서는 마우스만큼 정밀하지 않다 — 스냅·선 픽 반경을 3.5배(12→42px)로.
    // 이 호출이 빠지면 모바일에서도 데스크톱과 같은 12px 이라 스냅이 거의 안 잡힌다.
    if (ok) CAD_SetSnapTouchMode(true);
    // 모바일에서 치수는 도면 요소보다 "측정 도구" 다 — 줌아웃해도 값이 읽혀야 한다.
    // 켜면 치수 문자만 줌과 무관하게 화면상 같은 크기로 유지된다(선·화살표는 그대로).
    if (ok) CAD_SetDimensionTextScreenFixed(true);
    LOGI("nativeSurfaceCreated %dx%d engine=%d", w, h, ok);
    return ok ? JNI_TRUE : JNI_FALSE;
}

JNI(void, nativeSurfaceChanged)(JNIEnv*, jobject, jint w, jint h) {
    CAD_ResizeView(w, h);
}

// Surface 가 사라질 때(파일 선택기·홈 버튼·앱 전환) — **창만 떼고 엔진은 살려 둔다.**
// 예전엔 여기서 CAD_DestroyEngine 까지 해서, 돌아올 때마다 Vulkan 초기화·텍스처 로딩을 처음부터
// 다시 하느라 몇 초씩 걸리고 장면·되돌리기·카메라가 전부 날아갔다(데모 장면으로 초기화).
// 엔진이 창 떼기/붙이기를 지원한다(d084472: 스왑체인·Surface 만 내려놓음). 다시 붙이는 건
// nativeSurfaceCreated 의 CAD_AttachView — 엔진이 이미 있으면 그 호출이 새 창에 스왑체인을 다시 만든다.
// ⚠️ 창 해제는 DetachView **뒤에** — 엔진이 그 창의 Surface 를 먼저 내려놓아야 한다.
JNI(void, nativeSurfaceDestroyed)(JNIEnv*, jobject) {
    CAD_DetachView();
    if (g_window) { ANativeWindow_release(g_window); g_window = nullptr; }
    LOGI("nativeSurfaceDestroyed (engine kept)");
}

// 앱이 정말 끝날 때만(액티비티 finish) 엔진을 파괴한다. 회전처럼 액티비티만 다시 만들어지는
// 경우엔 부르지 않는다 — 엔진은 프로세스 전역이라 새 SurfaceView 가 그대로 다시 붙는다.
JNI(void, nativeDestroyEngine)(JNIEnv*, jobject) {
    CAD_DetachView();
    CAD_DestroyEngine();
    if (g_window) { ANativeWindow_release(g_window); g_window = nullptr; }
    LOGI("nativeDestroyEngine");
}

JNI(void, nativeTick)(JNIEnv*, jobject) {
    CAD_Tick();
}

// phase: 0=down 1=move 2=up, button: 0=left(선택) 1=right(궤도 회전) 2=middle(이동).
// (터치엔 버튼이 없어 Kotlin 이 손가락 수로 버튼을 정해 보낸다. 에뮬레이터 마우스는
//  실제 버튼 상태를 그대로 실어 보내므로 데스크톱과 같은 규약이 된다.)
JNI(void, nativeTouch)(JNIEnv*, jobject, jint phase, jint button, jfloat x, jfloat y) {
    switch (phase) {
        case 0: CAD_OnMouseDown(button, x, y, 0); break;
        case 1: CAD_OnMouseMove(x, y);            break;
        case 2: CAD_OnMouseUp(button, x, y, 0);   break;
        default: break;
    }
}

// 진행 중인 도구의 프롬프트("선: 시작점 클릭 …"). 도구가 없으면 빈 문자열.
// iOS 와 같은 용도 — 그리기 도구는 점 개수가 열려 있어(폴리선 등) 치수처럼 단계를 못 세므로
// **프롬프트가 비면 도구가 끝난 것**으로 보고 커서 모드를 해제한다.
JNI(jstring, nativePrompt)(JNIEnv* env, jobject) {
    char buf[256] = {};
    CAD_GetPrompt(buf, static_cast<int>(sizeof(buf)));
    return env->NewStringUTF(buf);
}

// 마우스 휠 — 에뮬레이터/블루투스 마우스에서 커서 중심 줌. 터치엔 휠이 없어 호출되지 않는다.
JNI(void, nativeScroll)(JNIEnv*, jobject, jfloat x, jfloat y, jfloat dy) {
    CAD_OnMouseWheel(x, y, 0.0, dy);
}

// ── 툴바 명령 ──
JNI(void, nativeAddCube)(JNIEnv*, jobject)      { CAD_RequestAddCube(); }
JNI(void, nativeCreateBox)(JNIEnv*, jobject)    { CAD_CreateBox(0, 0, 0, 1, 1, 1); }
JNI(void, nativeSelectAll)(JNIEnv*, jobject)    { CAD_RequestSelectAll(); }
JNI(void, nativeDeleteSelected)(JNIEnv*, jobject){ CAD_RequestDeleteSelected(); }
JNI(void, nativeClearAll)(JNIEnv*, jobject)     { CAD_RequestClearAll(); }
JNI(void, nativeZoomExtents)(JNIEnv*, jobject)  { CAD_RequestZoomExtents(); }
JNI(void, nativeSetView)(JNIEnv*, jobject, jint v) { CAD_RequestSetView(v); }
JNI(void, nativeUndo)(JNIEnv*, jobject)         { CAD_Undo(); }
JNI(void, nativeRedo)(JNIEnv*, jobject)         { CAD_Redo(); }
JNI(jint, nativeObjectCount)(JNIEnv*, jobject)  { return static_cast<jint>(CAD_GetObjectCount()); }

// ── 명령 이름으로 실행 ──
// 엔진 명령이 200개가 넘는데 위처럼 하나씩 JNI 를 만들 수는 없다. 이름을 넘기면 끝.
JNI(jboolean, nativeExecuteCommand)(JNIEnv* env, jobject, jstring name) {
    if (!name) return JNI_FALSE;
    const char* s = env->GetStringUTFChars(name, nullptr);
    const bool ok = s && CAD_ExecuteCommand(s);
    if (s) env->ReleaseStringUTFChars(name, s);
    return ok ? JNI_TRUE : JNI_FALSE;
}

// ── 측정 ──
// 선택만 하면 값이 나오는 경로. 점 찍기·스냅이 없어 모바일에서 가장 쓰기 쉽다.
// 잴 수 없는 종류(메시·문자)는 엔진이 합에서 건너뛰므로 0 이 올 수 있다.
JNI(jint,   nativeSelectedCount)(JNIEnv*, jobject)   { return static_cast<jint>(CAD_GetSelectedCount()); }
JNI(jfloat, nativeSelectionLength)(JNIEnv*, jobject) { return CAD_GetSelectionLength(); }
JNI(jfloat, nativeSelectionArea)(JNIEnv*, jobject)   { return CAD_GetSelectionArea(); }
// 진행 중인 치수 도구의 점 개수(비활성 -1). 커서 UI 가 클릭을 세는 대신 엔진에 물어본다.
JNI(jint, nativeDimensionPointCount)(JNIEnv*, jobject) { return CAD_GetDimensionPointCount(); }

// ── 파일 열기 ──
// 안드로이드 파일 선택기(SAF)는 content:// URI 를 주는데 엔진은 fopen 기반이라
// Kotlin 이 filesDir 로 복사한 뒤 그 **실제 경로**를 넘겨야 한다.
JNI(jboolean, nativeOpenFile)(JNIEnv* env, jobject, jstring path) {
    if (!path) return JNI_FALSE;
    const char* s = env->GetStringUTFChars(path, nullptr);
    const bool ok = s && CAD_OpenFile(s);
    if (s) env->ReleaseStringUTFChars(path, s);
    return ok ? JNI_TRUE : JNI_FALSE;
}

// ── 클립보드 ──
// 안드로이드 클립보드는 ClipboardManager(Kotlin)에만 있다. 엔진이 복사할 때
// Kotlin 쪽으로 올려 보내야 하는데, JNI 콜백은 스레드/전역참조 관리가 번거롭다.
// 그래서 **엔진이 복사한 마지막 문자열을 담아 두고** Kotlin 이 꺼내 가는 방식으로 둔다.
namespace {
    std::string g_lastCopied;
    void onEngineCopy(const char* utf8) { g_lastCopied = utf8 ? utf8 : ""; }
}
JNI(void, nativeInstallClipboard)(JNIEnv*, jobject) { CAD_SetOnCopyText(&onEngineCopy); }
JNI(jstring, nativeTakeCopiedText)(JNIEnv* env, jobject) {
    if (g_lastCopied.empty()) return nullptr;
    jstring s = env->NewStringUTF(g_lastCopied.c_str());
    g_lastCopied.clear();          // 한 번 가져가면 비운다
    return s;
}
JNI(void, nativePasteText)(JNIEnv* env, jobject, jstring text) {
    if (!text) return;
    const char* s = env->GetStringUTFChars(text, nullptr);
    if (s) { CAD_PasteText(s); env->ReleaseStringUTFChars(text, s); }
}

// ── 이미지 붙이기 ──
// 안드로이드 사진 선택(Intent)은 Uri 를 준다. Kotlin 에서 Bitmap 으로 읽어 RGBA 로
// 넘기는 게 경로를 다루는 것보다 안전하다(SAF 경로는 실제 파일이 아닐 수 있다).
JNI(jint, nativeAttachImageRgba)(JNIEnv* env, jobject, jbyteArray rgba, jint w, jint h) {
    if (!rgba || w <= 0 || h <= 0) return 0;
    jbyte* p = env->GetByteArrayElements(rgba, nullptr);
    if (!p) return 0;
    const uint32_t id = CAD_AttachImageFromMemory(
        reinterpret_cast<const unsigned char*>(p), w, h, "사진", 0.0f, 0, 0, 0);
    env->ReleaseByteArrayElements(rgba, p, JNI_ABORT);
    return static_cast<jint>(id);
}

// ── OCR 결과 주입 ──
// 인식은 Kotlin 의 ML Kit 이 한다(Tesseract 45MB 를 안 넣는 이유). 결과만 넘긴다.
// 사각형은 **그 이미지의 픽셀 좌표**(좌상단 원점).
JNI(void, nativeBeginOcrLines)(JNIEnv*, jobject, jint imageId) {
    CAD_BeginOcrLines(static_cast<uint32_t>(imageId));
}
JNI(void, nativeAddOcrLine)(JNIEnv* env, jobject, jstring text,
                            jint x0, jint y0, jint x1, jint y1) {
    if (!text) return;
    const char* s = env->GetStringUTFChars(text, nullptr);
    if (s) { CAD_AddOcrLine(s, x0, y0, x1, y1); env->ReleaseStringUTFChars(text, s); }
}
JNI(void, nativeEndOcrLines)(JNIEnv*, jobject) { CAD_EndOcrLines(); }

#undef JNI
} // extern "C"
