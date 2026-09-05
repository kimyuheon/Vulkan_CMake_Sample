package com.vulkancad.androidtest

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.view.View

// 가상 커서 십자선 — Vulkan SurfaceView **위에** 얹는 얇은 오버레이.
// SurfaceView 자체에는 그릴 수 없어서(표면은 Vulkan 이 소유) 별도 View 로 올린다.
// 선 몇 개라 비용은 무시할 수준이고, Vulkan 렌더 루프와 무관하게 갱신된다.
// (iOS 의 CAShapeLayer 십자선과 같은 역할)
class CursorOverlayView(context: Context) : View(context) {

    private var cx = -1f
    private var cy = -1f

    private val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
        strokeWidth = 3f
        color = Color.rgb(255, 214, 10)   // 노랑 — 도면 위에서 잘 보인다
    }

    init {
        // 터치는 아래 SurfaceView 가 받아야 한다. 이 뷰는 그리기 전용.
        isClickable = false
        isFocusable = false
    }

    fun setCursor(x: Float, y: Float) {
        cx = x; cy = y
        invalidate()
    }

    fun hideCursor() {
        cx = -1f; cy = -1f
        invalidate()
    }

    override fun onDraw(canvas: Canvas) {
        if (cx < 0f) return
        val r = 44f      // 십자선 팔 길이(px)
        val gap = 12f    // 가운데 빈 공간 — 조준점이 선에 묻히지 않게
        canvas.drawLine(cx - r, cy, cx - gap, cy, paint)
        canvas.drawLine(cx + gap, cy, cx + r, cy, paint)
        canvas.drawLine(cx, cy - r, cx, cy - gap, paint)
        canvas.drawLine(cx, cy + gap, cx, cy + r, paint)
        canvas.drawCircle(cx, cy, 6f, paint)
    }
}
