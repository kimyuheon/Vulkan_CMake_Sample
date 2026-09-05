package com.vulkancad.androidtest

import android.view.Surface

// libvulkancad.so 의 JNI 함수 (android_jni.cpp) 브릿지.
object CadNative {
    init { System.loadLibrary("vulkancad") }

    external fun nativeSetAssetPath(path: String)
    external fun nativeSurfaceCreated(surface: Surface, w: Int, h: Int): Boolean
    external fun nativeSurfaceChanged(w: Int, h: Int)
    external fun nativeSurfaceDestroyed()
    external fun nativeTick()
    // phase: 0=down 1=move 2=up, button: 0=left(선택) 1=right(궤도 회전) 2=middle(이동)
    external fun nativeTouch(phase: Int, button: Int, x: Float, y: Float)
    // 마우스 휠 — dy>0 이 확대. 터치엔 휠이 없어 마우스에서만 호출된다.
    external fun nativeScroll(x: Float, y: Float, dy: Float)
    // 진행 중 도구의 프롬프트. 비면 도구 없음 — 그리기 도구의 종료 신호로 쓴다.
    external fun nativePrompt(): String

    external fun nativeAddCube()
    external fun nativeCreateBox()
    external fun nativeSelectAll()
    external fun nativeDeleteSelected()
    external fun nativeClearAll()
    external fun nativeZoomExtents()
    external fun nativeSetView(v: Int)   // 0=Front 1=Top 2=Right 3=Iso
    external fun nativeUndo()
    external fun nativeRedo()
    external fun nativeObjectCount(): Int

    /** 엔진 명령을 이름으로 실행 — 하단 명령행에 치는 것과 같다.
     *  위 external fun 들은 자주 쓰는 것만 뽑아 둔 것이고 엔진엔 명령이 200개가 넘는다.
     *  없는 기능은 이걸로 부른다: nativeExecuteCommand("trim") */
    external fun nativeExecuteCommand(name: String): Boolean

    // 측정 — 선택만 하면 값이 나온다. 잴 수 없는 종류(메시·문자)는 0.
    external fun nativeSelectedCount(): Int
    external fun nativeSelectionLength(): Float
    external fun nativeSelectionArea(): Float
    /** 진행 중인 치수 도구의 점 개수. 비활성이면 -1. */
    external fun nativeDimensionPointCount(): Int

    // 파일 열기 — SAF 의 content:// 가 아니라 **실제 파일 경로**를 넘겨야 한다
    // (엔진이 fopen 기반). Kotlin 이 filesDir 로 복사한 뒤 그 경로를 준다.
    external fun nativeOpenFile(path: String): Boolean

    // ── 클립보드 ──
    // 안드로이드 클립보드는 Kotlin 쪽(ClipboardManager)에만 있다. 엔진이 복사하면
    // 네이티브에 담기고, 여기서 꺼내 ClipboardManager 에 넣는다.
    external fun nativeInstallClipboard()
    external fun nativeTakeCopiedText(): String?   // 없으면 null. 꺼내면 비워진다
    external fun nativePasteText(text: String)

    // ── 이미지 ──
    // Uri 로 받은 사진을 Bitmap → RGBA8 로 풀어 넘긴다(SAF 경로는 실제 파일이 아닐 수 있다).
    external fun nativeAttachImageRgba(rgba: ByteArray, w: Int, h: Int): Int

    // ── OCR (ML Kit 결과 주입) ──
    // 사각형은 그 이미지의 픽셀 좌표(좌상단 원점). 넘기는 순서 = 읽는 순서.
    external fun nativeBeginOcrLines(imageId: Int)
    external fun nativeAddOcrLine(text: String, x0: Int, y0: Int, x1: Int, y1: Int)
    external fun nativeEndOcrLines()
}
