package com.vulkancad.androidtest

import android.content.Context
import android.view.Choreographer
import android.view.MotionEvent
import android.view.ScaleGestureDetector
import android.view.SurfaceHolder
import android.view.SurfaceView
import android.view.ViewConfiguration

// SurfaceView 위에 Vulkan 엔진을 붙이고 Choreographer(vsync)로 매 프레임 Tick.
//  surface 생성/크기변경/파괴 → CAD_AttachView/ResizeView/DetachView (JNI)
//  터치 → CAD_OnMouse* (JNI)
class VulkanSurfaceView(context: Context) : SurfaceView(context),
    SurfaceHolder.Callback, Choreographer.FrameCallback {

    private var engineReady = false

    init { holder.addCallback(this) }

    override fun surfaceCreated(holder: SurfaceHolder) { /* 크기는 surfaceChanged 에서 */ }

    override fun surfaceChanged(holder: SurfaceHolder, format: Int, w: Int, h: Int) {
        engineReady = if (!engineReady) {
            CadNative.nativeSurfaceCreated(holder.surface, w, h)   // AttachView + CreateEngine
        } else {
            CadNative.nativeSurfaceChanged(w, h)                   // ResizeView
            true
        }
        if (engineReady) Choreographer.getInstance().postFrameCallback(this)
    }

    override fun surfaceDestroyed(holder: SurfaceHolder) {
        Choreographer.getInstance().removeFrameCallback(this)
        if (engineReady) { CadNative.nativeSurfaceDestroyed(); engineReady = false }
    }

    // 선택 측정값이 바뀔 때 호출된다(문자열이 실제로 달라질 때만). MainActivity 가 라벨 갱신.
    var onMeasureChanged: ((String) -> Unit)? = null
    private var lastMeasure = ""
    // 치수 도구 점 개수(비활성 -1). 값이 바뀔 때만 알린다.
    var onDimPointCountChanged: ((Int) -> Unit)? = null
    private var lastDimCount = -2

    // 도구 프롬프트 — 값이 바뀔 때만 알린다. 그리기 도구는 점 개수가 열려 있어 치수처럼
    // 단계를 못 세므로, MainActivity 가 "프롬프트가 비면 도구 종료" 로 커서 모드를 푼다.
    var onPromptChanged: ((String) -> Unit)? = null
    private var lastPrompt = ""

    override fun doFrame(frameTimeNanos: Long) {
        if (!engineReady) return
        CadNative.nativeTick()
        updateMeasure()
        val dc = CadNative.nativeDimensionPointCount()
        if (dc != lastDimCount) { lastDimCount = dc; onDimPointCountChanged?.invoke(dc) }
        val pr = CadNative.nativePrompt()
        if (pr != lastPrompt) { lastPrompt = pr; onPromptChanged?.invoke(pr) }
        Choreographer.getInstance().postFrameCallback(this)   // 다음 vsync 예약
    }

    // 선택 → 길이·면적. 점 찍기·스냅 없이 탭 한 번으로 값이 나오는 경로.
    // 매 프레임 읽되 **표시 문자열이 바뀔 때만** 콜백 — 60fps 로 TextView 를 건드리면
    // 레이아웃이 매 프레임 다시 돌아 렌더와 경합한다.
    private fun updateMeasure() {
        val n = CadNative.nativeSelectedCount()
        val text = if (n <= 0) "" else buildString {
            append("선택 ").append(n)
            val len = CadNative.nativeSelectionLength()
            val area = CadNative.nativeSelectionArea()
            if (len > 0f) append("  ·  길이 ").append(fmt(len))
            if (area > 0f) append("  ·  면적 ").append(fmt(area)).append("²")
        }
        if (text != lastMeasure) { lastMeasure = text; onMeasureChanged?.invoke(text) }
    }

    // 값 크기에 따라 자릿수를 줄여 좁은 화면에서도 읽히게 한다.
    private fun fmt(v: Float): String = when {
        v >= 100f -> String.format("%.0f", v)
        v >= 10f  -> String.format("%.1f", v)
        else      -> String.format("%.2f", v)
    }

    // ⭐ 1손가락 = 이동(pan), 2손가락 = 궤도회전. (iOS 와 동일 규약)
    //    3D 뷰어 관례는 1손가락 궤도회전이지만 이건 CAD 다 — 도면에서 가장 자주 하는 건
    //    이동·줌이고, 지도/PDF 앱과 같은 감각이라 덜 어색하다. 치수처럼 점을 찍는 도구에서
    //    실수로 뷰가 돌아가는 사고도 막는다.
    //      • 짧은 탭(슬롭 이내에서 뗌) → 선택      (button 0 = left)
    //      • 1손가락 드래그            → 이동      (button 2 = middle)
    //      • 2손가락 드래그            → 궤도회전  (button 1 = right)
    //    ⚠️ 마우스(에뮬레이터)는 pointerCount 가 항상 1 이라 이 규약으로는 회전이 안 된다.
    //       그래서 마우스로 판별되면 데스크톱 규약으로 갈아탄다 — mouseButtonOf() 참조.
    /** [회전] 토글 상태 — true 면 1손가락 드래그 = 궤도회전 (MainActivity 버튼이 바꾼다). */
    var orbitMode = false

    private val touchSlop = ViewConfiguration.get(context).scaledTouchSlop
    private var downX = 0f
    private var downY = 0f
    private var activeButton = -1        // 드래그 중인 버튼 (-1 = 드래그 아님)

    // ─── 가상 커서 (도구 사용 중) ────────────────────────────────
    // 손가락이 목표를 가려 스냅 심볼이 안 보이는 문제를 구조적으로 없앤다. 커서는 손가락과
    // **분리**돼 있고 드래그는 **상대 이동**이라 화면 아무 데서나 조작해도 된다.
    // 데스크톱 마우스와 같은 모델이라 도구를 터치용으로 다시 만들 필요가 없다. (iOS 와 동일)
    var cursorOverlay: CursorOverlayView? = null
    private var cursorMode = false
    private var cursorX = 0f
    private var cursorY = 0f
    private val cursorGain = 0.6f        // 손가락보다 천천히 → 픽셀 단위 조준 가능

    fun setCursorMode(on: Boolean) {
        cursorMode = on
        if (on) {
            if (cursorX == 0f && cursorY == 0f) { cursorX = width * 0.5f; cursorY = height * 0.5f }
            cursorOverlay?.setCursor(cursorX, cursorY)
            CadNative.nativeTouch(1, BTN_LEFT, cursorX, cursorY)   // 호버 → 스냅 미리보기
        } else {
            cursorOverlay?.hideCursor()
        }
    }

    /** [선택] 버튼 — 현재 커서 자리를 클릭으로 확정. */
    fun commitCursorClick() {
        if (!cursorMode) return
        CadNative.nativeTouch(0, BTN_LEFT, cursorX, cursorY)
        CadNative.nativeTouch(2, BTN_LEFT, cursorX, cursorY)
    }

    private fun moveCursorBy(dx: Float, dy: Float) {
        cursorX = (cursorX + dx * cursorGain).coerceIn(0f, width.toFloat())
        cursorY = (cursorY + dy * cursorGain).coerceIn(0f, height.toFloat())
        cursorOverlay?.setCursor(cursorX, cursorY)
        CadNative.nativeTouch(1, BTN_LEFT, cursorX, cursorY)   // 호버 = 스냅 갱신
    }

    private companion object {
        const val BTN_LEFT = 0
        const val BTN_RIGHT = 1          // 엔진 궤도회전
        const val BTN_MIDDLE = 2         // 엔진 이동(pan)
    }

    // ─── 마우스(에뮬레이터 / 블루투스) ──────────────────────────────
    // 터치엔 버튼이 없어 손가락 수로 모드를 정하지만, 마우스는 pointerCount 가 **항상 1** 이라
    // 그 규약으로는 궤도 회전을 만들 수 없다(에뮬레이터에서 pan 밖에 안 되던 원인).
    // 마우스로 판별되면 데스크톱과 같은 규약을 쓴다 — 좌=선택 · 우=회전 · 중=이동 · 휠=줌.
    private fun isMouse(e: MotionEvent): Boolean =
        e.getToolType(0) == MotionEvent.TOOL_TYPE_MOUSE

    /** 마우스면 눌린 버튼에 맞는 엔진 버튼, 터치면 null(= 손가락 수 규약을 그대로 씀). */
    private fun mouseButtonOf(e: MotionEvent): Int? {
        if (!isMouse(e)) return null
        return when {
            e.buttonState and MotionEvent.BUTTON_SECONDARY != 0 -> BTN_RIGHT    // 우 = 궤도회전
            e.buttonState and MotionEvent.BUTTON_TERTIARY != 0  -> BTN_MIDDLE   // 중 = 이동
            else -> BTN_MIDDLE                                                  // 좌 드래그 = 이동
        }
    }

    // ─── 핀치 줌 ────────────────────────────────────────────────
    // iOS 는 UIPinchGestureRecognizer 로 진작 되고 있었는데 안드로이드엔 아예 없었다.
    // 도구 진행 중엔 2손가락이 pan 이라, 줌은 핀치가 유일한 수단이다.
    // 핀치와 드래그는 **동시에** 받는다 (iOS "2손가락 궤도회전과 pinch 는 동시 인식" 과 동일).
    // ⚠️ 처음엔 핀치 중 ACTION_MOVE 를 막았는데, 손가락 간격이 slop 만큼만 흔들려도 핀치로
    //    판정돼 2손가락 드래그가 손을 뗄 때까지 얼어붙었다. 줌하면서 돌리는 게 CAD 표준이다.
    private val scaleDetector = ScaleGestureDetector(context,
        object : ScaleGestureDetector.SimpleOnScaleGestureListener() {
            override fun onScale(d: ScaleGestureDetector): Boolean {
                // scaleFactor 는 직전 대비 배율(1.0 = 변화 없음). 확대(>1) = 양수 휠.
                val delta = (d.scaleFactor - 1f) * 8f   // 감도 계수 — iOS 와 동일
                if (kotlin.math.abs(delta) > 0.0001f) {
                    CadNative.nativeScroll(d.focusX, d.focusY, delta)
                }
                return true
            }
        })

    override fun onGenericMotionEvent(e: MotionEvent): Boolean {
        if (e.actionMasked == MotionEvent.ACTION_SCROLL) {
            CadNative.nativeScroll(e.x, e.y, e.getAxisValue(MotionEvent.AXIS_VSCROLL))
            return true
        }
        // ─── 보조 버튼(우/중) 드래그 ───
        // 안드로이드는 마우스의 **주(좌) 버튼만** "터치" 로 본다. 우/중 버튼은 ACTION_DOWN/MOVE 가
        // 아니라 ACTION_BUTTON_PRESS/RELEASE + 버튼이 눌린 채의 ACTION_HOVER_MOVE 로 여기에 온다.
        // onTouchEvent 에서만 buttonState 를 읽으면 우클릭 드래그(궤도회전)가 통째로 빠진다.
        when (e.actionMasked) {
            MotionEvent.ACTION_BUTTON_PRESS -> {
                val btn = when (e.actionButton) {
                    MotionEvent.BUTTON_SECONDARY -> BTN_RIGHT    // 우 = 궤도회전
                    MotionEvent.BUTTON_TERTIARY  -> BTN_MIDDLE   // 중 = 이동
                    else -> return super.onGenericMotionEvent(e) // 좌는 onTouchEvent 경로
                }
                if (activeButton < 0) beginDrag(btn, e.x, e.y)
                return true
            }
            MotionEvent.ACTION_BUTTON_RELEASE -> {
                if (activeButton >= 0 && activeButton != BTN_LEFT) endDrag(e.x, e.y)
                return true
            }
            MotionEvent.ACTION_HOVER_MOVE -> {
                // 보조 버튼 드래그 중이면 이동으로, 아니면 호버(스냅 미리보기)로.
                // 가상 커서 좌표도 따라가게 둔다 — 마우스로 조준한 뒤 [선택] 버튼을 눌러도
                // 커서가 화면 중앙에 멈춰 있어 엉뚱한 점이 찍히는 일이 없게.
                cursorX = e.x; cursorY = e.y
                if (activeButton >= 0 && activeButton != BTN_LEFT) {
                    CadNative.nativeTouch(1, activeButton, e.x, e.y)
                } else {
                    CadNative.nativeTouch(1, BTN_LEFT, e.x, e.y)
                }
                return true
            }
        }
        return super.onGenericMotionEvent(e)
    }

    // 손가락 여러 개면 중심점을 좌표로 쓴다(2손가락 궤도회전이 흔들리지 않게).
    private fun centroidX(e: MotionEvent): Float {
        var s = 0f; for (i in 0 until e.pointerCount) s += e.getX(i); return s / e.pointerCount
    }
    private fun centroidY(e: MotionEvent): Float {
        var s = 0f; for (i in 0 until e.pointerCount) s += e.getY(i); return s / e.pointerCount
    }

    private fun endDrag(x: Float, y: Float) {
        if (activeButton >= 0) {
            CadNative.nativeTouch(2, activeButton, x, y)
            activeButton = -1
        }
    }

    private fun beginDrag(button: Int, x: Float, y: Float) {
        activeButton = button
        CadNative.nativeTouch(0, button, x, y)
        CadNative.nativeTouch(1, button, x, y)
    }

    override fun onTouchEvent(e: MotionEvent): Boolean {
        scaleDetector.onTouchEvent(e)   // 줌은 여기서, 드래그는 아래에서 — 둘 다 흘러간다
        when (e.actionMasked) {
            MotionEvent.ACTION_DOWN -> {
                downX = e.x; downY = e.y; activeButton = -1
            }
            // 손가락이 늘거나 줄면 진행 중인 드래그를 끝내고 새 모드로 다시 시작한다.
            // (1↔2 손가락 전환에서 버튼이 섞이지 않게)
            MotionEvent.ACTION_POINTER_DOWN, MotionEvent.ACTION_POINTER_UP -> {
                endDrag(centroidX(e), centroidY(e))
                downX = centroidX(e); downY = centroidY(e)
            }
            MotionEvent.ACTION_MOVE -> {
                val twoFinger = e.pointerCount >= 2
                val x = if (twoFinger) centroidX(e) else e.x
                val y = if (twoFinger) centroidY(e) else e.y

                // 도구 사용 중이면 1손가락은 **커서 상대 이동**(손가락과 분리 → 가림 없음).
                // 2손가락 pan · 핀치 줌은 그대로라 도구 진행 중에도 화면 조작이 된다.
                // 마우스는 포인터 자체가 이미 정밀하므로 가상 커서를 태우지 않는다.
                if (cursorMode && !twoFinger && !isMouse(e)) {
                    moveCursorBy(x - downX, y - downY)
                    downX = x; downY = y
                    return true
                }

                // 커서 모드에선 2손가락이 **pan** 이다. 1손가락이 커서에 묶여 있어 2손가락이
                // 유일하게 남은 화면 조작 수단인데, 여기서도 궤도회전을 하면 점을 찍는 내내
                // pan 을 할 방법이 없다. 점 찍는 중엔 회전보다 "화면 밖 목표를 끌어오는" pan 이
                // 필요하고, 시점이 돌면 스케치 평면 기준이 흔들려 오히려 방해가 된다.
                // [회전] 토글 — 1손가락 드래그를 궤도회전으로. 에뮬레이터는 마우스 장치가 없어
                // (virtio_input_multi_touch 만 있음) 우클릭이 앱에 닿지 않고 2손가락도 못 만든다.
                // 회전할 수단이 아예 없으므로 화면 버튼으로 모드를 바꾼다. 커서 모드에선 무시(점 찍기 우선).
                val orbitByToggle = orbitMode && !twoFinger && !cursorMode
                // 3손가락 = 궤도회전, **항상** (iOS 와 동일). 커서 모드에선 2손가락이 pan 이라
                // 회전 수단이 없었다 — 점 찍는 중 TOP 에서 살짝 기울여 볼 일이 흔하다.
                val threeFinger = e.pointerCount >= 3
                val want = mouseButtonOf(e)
                    ?: if (threeFinger || (twoFinger && !cursorMode) || orbitByToggle) BTN_RIGHT
                       else BTN_MIDDLE
                if (activeButton < 0) {
                    val dx = x - downX; val dy = y - downY
                    if (dx * dx + dy * dy > touchSlop * touchSlop) beginDrag(want, x, y)
                } else if (activeButton != want) {
                    endDrag(x, y); beginDrag(want, x, y)   // 손가락 수가 바뀐 경우
                } else {
                    CadNative.nativeTouch(1, activeButton, x, y)
                }
            }
            MotionEvent.ACTION_UP -> {
                if (activeButton >= 0) {
                    endDrag(e.x, e.y)
                } else if (!cursorMode || isMouse(e)) {
                    // 커서 모드에선 **터치** 탭을 무시 — 안 그러면 손가락 위치로 클릭이 나가
                    // 커서가 가리키던 곳이 아닌 엉뚱한 점이 찍힌다. 확정은 [선택] 버튼만.
                    // 마우스는 가림이 없고 포인터가 곧 조준점이라 그대로 클릭시킨다.
                    cursorX = e.x; cursorY = e.y
                    CadNative.nativeTouch(0, BTN_LEFT, e.x, e.y)   // 클릭 = 선택/점 확정
                    CadNative.nativeTouch(2, BTN_LEFT, e.x, e.y)
                }
            }
            MotionEvent.ACTION_CANCEL -> endDrag(e.x, e.y)
            else -> return false
        }
        return true
    }
}
