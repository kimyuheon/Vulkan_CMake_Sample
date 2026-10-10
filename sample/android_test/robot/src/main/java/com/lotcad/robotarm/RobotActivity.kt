package com.lotcad.robotarm

import android.app.Activity
import android.graphics.Color
import android.os.Bundle
import android.view.Gravity
import android.view.ViewGroup
import android.widget.Button
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.SeekBar
import android.widget.TextView
import com.lotcad.androidtest.CadNative
import com.lotcad.androidtest.EngineAssets
import com.lotcad.androidtest.VulkanSurfaceView

// 로봇 팔 샘플 — 위: 3D 뷰, 아래: 관절 슬라이더 + [재생] [홈] [맞춤].
// 무엇을 움직일지(관절 → 조작 항목, 자동 재생)는 공용 로직 robot_demo 가 정하고,
// 여기는 그것을 슬라이더로 보여 주기만 한다. iOS RobotArmView 와 같은 구성.
class RobotActivity : Activity() {

    private companion object {
        const val STEPS = 1000            // SeekBar 는 정수라 범위를 1000 칸으로 나눈다
        const val SYNC_EVERY_FRAMES = 6   // 재생 중 슬라이더를 따라 움직이는 주기(≈10Hz)
    }

    private class Row(val bar: SeekBar, val value: TextView, val min: Float, val max: Float, val unit: String)

    private lateinit var renderView: VulkanSurfaceView
    private lateinit var controlsBox: LinearLayout
    private lateinit var playBtn: Button
    private val rows = mutableListOf<Row>()
    private var frame = 0

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        EngineAssets.install(this)

        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setBackgroundColor(Color.rgb(24, 26, 30))
        }

        renderView = VulkanSurfaceView(this).apply {
            orbitMode = true   // 로봇은 돌려 보는 게 주목적 — 1손가락 = 궤도회전, 2손가락 = 회전/이동
            onEngineReady = { onEngineReady() }
            onBeforeTick = { dt -> RobotNative.update(dt); syncFromEngine() }
        }
        root.addView(renderView, LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT, 0, 1.2f))

        val buttons = LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL }
        playBtn = Button(this).apply {
            text = "▶ 재생"
            setOnClickListener {
                RobotNative.setPlaying(!RobotNative.isPlaying())
                updatePlayButton()
            }
        }
        buttons.addView(playBtn, LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f))
        buttons.addView(Button(this).apply {
            text = "홈 자세"
            setOnClickListener { RobotNative.home(); updatePlayButton(); syncAll() }
        }, LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f))
        buttons.addView(Button(this).apply {
            text = "화면 맞춤"
            setOnClickListener { CadNative.nativeZoomExtents() }
        }, LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f))
        root.addView(buttons)

        controlsBox = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(24, 8, 24, 24)
        }
        root.addView(ScrollView(this).apply { addView(controlsBox) },
            LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, 0, 1f))

        setContentView(root)
    }

    // 엔진이 Surface 에 붙을 때마다 불린다. 로봇은 한 번만 불러온다(공용 로직이 판단).
    private fun onEngineReady() {
        if (!RobotNative.load()) {
            controlsBox.removeAllViews()
            controlsBox.addView(TextView(this).apply {
                text = "demo_arm.urdf 를 불러오지 못했습니다 (logcat -s LotCAD-stdout)"
                setTextColor(Color.rgb(255, 120, 120))
            })
            return
        }
        if (rows.isEmpty()) buildRows()
        syncAll()
    }

    private fun buildRows() {
        controlsBox.removeAllViews()
        rows.clear()
        for (i in 0 until RobotNative.controlCount()) {
            val lo = RobotNative.controlMin(i)
            val hi = RobotNative.controlMax(i)
            val unit = RobotNative.controlUnit(i)

            val header = LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL }
            header.addView(TextView(this).apply {
                text = RobotNative.controlLabel(i)
                setTextColor(Color.WHITE)
                textSize = 15f
            }, LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f))
            val value = TextView(this).apply {
                setTextColor(Color.rgb(255, 170, 70))
                textSize = 15f
                gravity = Gravity.END
            }
            header.addView(value)

            val bar = SeekBar(this).apply { max = STEPS }   // SeekBar.max (칸 수)
            val row = Row(bar, value, lo, hi, unit)
            bar.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
                override fun onProgressChanged(sb: SeekBar, progress: Int, fromUser: Boolean) {
                    if (!fromUser) return   // 재생 중 엔진 값으로 옮긴 것은 다시 넣지 않는다
                    val v = lo + (hi - lo) * progress / STEPS
                    RobotNative.setControl(i, v)   // 손으로 잡으면 재생은 멈춘다
                    showValue(row, v)
                    updatePlayButton()
                }
                override fun onStartTrackingTouch(sb: SeekBar) {}
                override fun onStopTrackingTouch(sb: SeekBar) {}
            })
            controlsBox.addView(header)
            controlsBox.addView(bar)
            rows += row
        }
    }

    private fun showValue(row: Row, v: Float) {
        row.value.text = if (row.unit == "%") "%.0f %%".format(v) else "%.0f°".format(v)
    }

    private fun syncAll() {
        rows.forEachIndexed { i, row ->
            val v = RobotNative.getControl(i)
            val span = row.max - row.min
            row.bar.progress = if (span > 0f) ((v - row.min) / span * STEPS).toInt() else 0
            showValue(row, v)
        }
    }

    // 재생 중엔 슬라이더가 로봇을 따라 움직이게 — 매 프레임은 레이아웃 비용이 커서 몇 프레임에 한 번.
    private fun syncFromEngine() {
        if (!RobotNative.isPlaying()) return
        if (++frame % SYNC_EVERY_FRAMES == 0) syncAll()
    }

    private fun updatePlayButton() {
        playBtn.text = if (RobotNative.isPlaying()) "⏸ 정지" else "▶ 재생"
    }

    // 엔진은 Surface 가 사라져도 살려 둔다 — 앱이 정말 끝날 때만 파괴 (MainActivity 와 같은 규약).
    override fun onDestroy() {
        if (isFinishing) CadNative.nativeDestroyEngine()
        super.onDestroy()
    }
}
