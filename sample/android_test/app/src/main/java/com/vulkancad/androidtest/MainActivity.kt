package com.vulkancad.androidtest

import android.app.Activity
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.provider.OpenableColumns
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.HorizontalScrollView
import android.widget.FrameLayout
import android.widget.LinearLayout
import android.widget.TextView
import java.io.File

// 상단 툴바(버튼) + Vulkan SurfaceView. 에셋(모델/폰트/텍스처)을 filesDir 로 추출한 뒤
// nativeSetAssetPath 로 엔진에 알려준다(엔진은 상대경로 fopen 사용).
class MainActivity : Activity() {

    private companion object { const val REQ_OPEN_FILE = 1001 }

    private var renderView: VulkanSurfaceView? = null
    private lateinit var cursorBar: LinearLayout
    private lateinit var cursorPickBtn: Button
    private var cursorStep = 0
    // 숫자패드 (5열×3줄, iOS 와 동일). 치수 도구에선 숨긴다 — 값을 안 받는다.
    private lateinit var numPad: LinearLayout
    private lateinit var numValue: TextView
    private var numInput = ""

    // 커서가 마우스를 대신하므로 데스크톱 도구 흐름(점1 → 점2 → 치수선 위치)을 그대로 쓴다.
    private fun beginCursorTool() {
        cursorStep = 0
        cursorPickBtn.text = "선택 1"
        cursorBar.visibility = View.VISIBLE
        // 패드는 프롬프트가 뜨는 틱에 연다(onPrompt) — 명령이 큐로 가서 지금은 치수인지 모른다.
        numInput = ""; numValue.text = " "; numPad.visibility = View.GONE
        renderView?.setCursorMode(true)
    }

    private fun endCursorTool() {
        cursorStep = 0
        cursorBar.visibility = View.GONE
        numPad.visibility = View.GONE
        renderView?.setCursorMode(false)
    }

    /** 프롬프트 반영 — 그리기 도구는 점 개수가 열려 있어(폴리선 등) 치수처럼 단계를 못 센다.
     *  그래서 **프롬프트가 비면 도구가 끝난 것**으로 보고 커서 모드를 푼다 (iOS syncCursorTool 과 동일). */
    private fun onPrompt(p: String) {
        if (!cursorBarVisible()) return
        if (CadNative.nativeDimensionPointCount() >= 0) { numPad.visibility = View.GONE; return }  // 치수: onDimCount 가 맡음
        if (p.isEmpty()) { endCursorTool(); return }
        numPad.visibility = View.VISIBLE
    }

    private fun commitCursorPoint() {
        // 단계는 세지 않는다 — 다음 틱에 엔진 값으로 갱신된다.
        // 클라이언트가 세면 엔진이 클릭을 거부할 때(각도에서 선이 아닌 곳 등) 어긋나고,
        // 도구가 안 끝나 선택까지 막히는 사고가 난다.
        renderView?.commitCursorClick()
    }

    /** 엔진의 치수 점 개수 반영. -1(도구 종료)이면 커서 모드도 자동 해제. */
    private fun onDimCount(n: Int) {
        if (!cursorBarVisible()) return
        if (n < 0) { endCursorTool(); return }
        cursorStep = n
        cursorPickBtn.text = when (n) { 0 -> "선택 1"; 1 -> "선택 2"; else -> "선택" }
    }

    private fun cursorBarVisible() = ::cursorBar.isInitialized && cursorBar.visibility == View.VISIBLE

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // 1) APK assets → filesDir 추출 → 엔진 asset 경로 지정
        extractAssets()
        CadNative.nativeSetAssetPath(filesDir.absolutePath)

        // 2) 레이아웃: 세로(툴바 + 렌더뷰)
        val root = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL }

        // 치수 종류 줄 — "측정" 을 눌렀을 때만 보인다(점진적 노출, iOS 와 동일 구성).
        // 치수는 전부 "점 찍기" 방식이라 대화상자 없이 모바일에서 그대로 동작한다.
        val dimBar = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            visibility = View.GONE
        }
        fun addDimBtn(label: String, cmd: String) {
            dimBar.addView(Button(this).apply {
                text = label
                setOnClickListener {
                    CadNative.nativeExecuteCommand(cmd)
                    dimBar.visibility = View.GONE   // 도구 활성 후엔 3D 뷰를 봐야 한다
                    beginCursorTool()
                }
            })
        }
        addDimBtn("정렬", "dim")    // 두 점 사이 실제 거리
        addDimBtn("일반", "dli")    // 수평·수직
        addDimBtn("각도", "dan")    // 선 2개 → 호 위치 (오토캐드식 3클릭)
        addDimBtn("반지름", "dra")
        addDimBtn("지름", "ddi")
        dimBar.addView(Button(this).apply {
            text = "✕"; setOnClickListener { dimBar.visibility = View.GONE }
        })

        // 그리기 줄 — "그리기" 를 눌렀을 때만 보인다 (iOS drawMenuRow 와 동일 구성).
        // 2D 는 전부 점 찍기, 3D 도 클릭+값이라 커서·숫자패드 흐름이 같다.
        //   박스: 코너 → 반대 코너(또는 가로,세로) → 높이   돌출: 닫힌 2D 선택 후 높이   밀당: 면 → 거리
        // 구·원기둥·원뿔·토러스는 즉시 생성이라 커서 도구로 들어가지 않는다.
        val drawBar = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            visibility = View.GONE
        }
        fun addDrawBtn(label: String, cmd: String, cursorTool: Boolean = true) {
            drawBar.addView(Button(this).apply {
                text = label
                setOnClickListener {
                    CadNative.nativeExecuteCommand(cmd)
                    drawBar.visibility = View.GONE   // 도구 활성 후엔 3D 뷰를 봐야 한다
                    if (cursorTool) beginCursorTool()
                }
            })
        }
        addDrawBtn("선", "line");     addDrawBtn("사각형", "rec");    addDrawBtn("원", "circle")
        addDrawBtn("호", "arc");      addDrawBtn("폴리선", "polyline"); addDrawBtn("다각형", "polygon")
        addDrawBtn("박스", "box");    addDrawBtn("돌출", "x");        addDrawBtn("밀당", "pp")
        addDrawBtn("구", "sphere", cursorTool = false)
        addDrawBtn("원기둥", "cylinder", cursorTool = false)
        addDrawBtn("원뿔", "cone", cursorTool = false)
        addDrawBtn("토러스", "torus", cursorTool = false)
        drawBar.addView(Button(this).apply {
            text = "✕"; setOnClickListener { drawBar.visibility = View.GONE }
        })

        // 뷰 종류 줄 — "뷰" 를 눌렀을 때만 (그리기·측정과 같은 점진적 노출, iOS 와 동일 구성).
        // TOP 으로 놓고 살짝 기울인 뒤 박스를 그리는 데스크톱 흐름을 한 탭으로. 고르면 줄은 닫힌다.
        val viewBar = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            visibility = View.GONE
        }
        fun addViewBtn(label: String, action: () -> Unit) {
            viewBar.addView(Button(this).apply {
                text = label
                setOnClickListener { action(); viewBar.visibility = View.GONE }
            })
        }
        addViewBtn("Top")   { CadNative.nativeSetView(1) }
        addViewBtn("Front") { CadNative.nativeSetView(0) }
        addViewBtn("Right") { CadNative.nativeSetView(2) }
        addViewBtn("Iso")   { CadNative.nativeSetView(3) }
        addViewBtn("투영")  { CadNative.nativeExecuteCommand("proj") }   // 직교 ↔ 원근
        viewBar.addView(Button(this).apply {
            text = "✕"; setOnClickListener { viewBar.visibility = View.GONE }
        })

        val bar = LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL }
        fun addBtn(label: String, action: () -> Unit) {
            bar.addView(Button(this).apply { text = label; setOnClickListener { action() } })
        }
        addBtn("열기")     { pickFileToOpen() }
        // 그리기·측정·뷰 줄은 서로 배타 — 둘 이상 열리면 줄이 겹쳐 뷰가 좁아진다.
        fun toggleOnly(target: LinearLayout) {
            val open = target.visibility == View.GONE
            drawBar.visibility = View.GONE; dimBar.visibility = View.GONE; viewBar.visibility = View.GONE
            if (open) target.visibility = View.VISIBLE
        }
        addBtn("그리기")   { toggleOnly(drawBar) }
        addBtn("측정")     { toggleOnly(dimBar) }
        addBtn("뷰")       { toggleOnly(viewBar) }
        addBtn("큐브")     { CadNative.nativeAddCube() }
        addBtn("전체선택") { CadNative.nativeSelectAll() }
        addBtn("삭제")     { CadNative.nativeDeleteSelected() }
        addBtn("줌")       { CadNative.nativeZoomExtents() }
        addBtn("Undo")     { CadNative.nativeUndo() }
        // [회전] 토글 — 1손가락 드래그를 pan ↔ 궤도회전으로. 리스너는 renderView 생성 뒤에 단다.
        // 에뮬레이터는 마우스 장치가 없어(virtio_input_multi_touch 뿐) 우클릭도 2손가락도
        // 못 만든다 — 회전할 수단이 이것뿐이다. 실기에서도 한 손 조작에 쓸모 있다.
        val orbitBtn = Button(this).apply { text = "회전" }
        bar.addView(orbitBtn)

        val barScroll = HorizontalScrollView(this).apply { addView(bar) }
        root.addView(barScroll, LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT))
        root.addView(HorizontalScrollView(this).apply { addView(dimBar) },
            LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT))
        root.addView(HorizontalScrollView(this).apply { addView(drawBar) },
            LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT))
        root.addView(HorizontalScrollView(this).apply { addView(viewBar) },
            LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT))

        // 선택한 것의 길이·면적 — 선택이 있을 때만 보인다.
        // 점 찍기·스냅 없이 탭 한 번으로 값이 나오는 경로라 모바일에서 가장 쓰기 쉽다.
        val measureLabel = TextView(this).apply {
            visibility = View.GONE
            setTextColor(0xFF4CD964.toInt())
            setPadding(24, 12, 24, 12)
        }
        root.addView(measureLabel, LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT))

        // 숫자패드 — 5열×3줄 고정 (iOS 와 동일). ↵ 는 값칸 옆에 두어 격자가 15칸에 딱 맞는다.
        // 값은 엔진 명령행과 같은 입구(nativeExecuteCommand)로 들어간다 — 접두 없으면 상대,
        // '=' 절대, 단일 값은 커서 방향 거리(DDE). 치수 도구에선 onPrompt 가 숨긴다.
        numValue = TextView(this).apply {
            text = " "                                  // 비어도 자리를 지켜 키가 위아래로 안 흔들리게
            textSize = 18f
            setTextColor(0xFFFFD60A.toInt())
            setPadding(24, 12, 24, 12)
        }
        val numEnter = Button(this).apply {
            text = "↵"
            setOnClickListener {
                if (numInput.isNotEmpty()) {
                    CadNative.nativeExecuteCommand(numInput)
                    numInput = ""; numValue.text = " "
                }
            }
        }
        numPad = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL; visibility = View.GONE }
        numPad.addView(LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            addView(numValue, LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f))
            addView(numEnter)
        })
        val numRows = listOf(listOf("1", "2", "3", "4", "5"),
                             listOf("6", "7", "8", "9", "0"),
                             listOf(".", ",", "-", "=", "⌫"))
        for (row in numRows) {
            val line = LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL }
            for (k in row) {
                line.addView(Button(this).apply {
                    text = k
                    setOnClickListener {
                        if (k == "⌫") { if (numInput.isNotEmpty()) numInput = numInput.dropLast(1) }
                        else numInput += k
                        numValue.text = if (numInput.isEmpty()) " " else numInput
                    }
                }, LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f))   // 5등분
            }
            numPad.addView(line)
        }
        root.addView(numPad, LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT))

        // 커서 도구 줄 — 버튼은 하나이고 **문구만 단계에 따라 바뀐다**(선택 1 → 선택 2 → 선택).
        cursorBar = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            visibility = View.GONE
        }
        cursorPickBtn = Button(this).apply {
            text = "선택 1"
            setOnClickListener { commitCursorPoint() }
        }
        cursorBar.addView(cursorPickBtn)
        cursorBar.addView(Button(this).apply {
            text = "취소"
            setOnClickListener { CadNative.nativeExecuteCommand("esc"); endCursorTool() }
        })
        root.addView(cursorBar, LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT))

        val renderView = VulkanSurfaceView(this)
        // 값이 바뀔 때만 불린다(매 프레임 아님) — 레이아웃이 렌더와 경합하지 않게.
        renderView.onMeasureChanged = { text ->
            runOnUiThread {
                measureLabel.text = text
                measureLabel.visibility = if (text.isEmpty()) View.GONE else View.VISIBLE
            }
        }
        renderView.onDimPointCountChanged = { n -> runOnUiThread { onDimCount(n) } }
        renderView.onPromptChanged = { p -> runOnUiThread { onPrompt(p) } }
        this.renderView = renderView
        // [회전] 토글 — 켜지면 문구로 상태를 보인다 (●). 1손가락 드래그 = pan ↔ 궤도회전.
        orbitBtn.setOnClickListener {
            renderView.orbitMode = !renderView.orbitMode
            orbitBtn.text = if (renderView.orbitMode) "회전 ●" else "회전"
        }

        // SurfaceView 표면엔 그릴 수 없어서 십자선은 위에 얹는 별도 View 로 둔다.
        val overlay = CursorOverlayView(this)
        renderView.cursorOverlay = overlay
        val stack = FrameLayout(this).apply {
            addView(renderView, FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT))
            addView(overlay, FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT))
        }
        root.addView(stack, LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT, 0, 1f))  // 남은 공간 전부

        setContentView(root)
    }

    // ── 파일 열기 (SAF) ──
    // 엔진은 fopen 기반이라 content:// URI 를 그대로 못 쓴다. 고른 문서를 filesDir 로
    // 복사한 뒤 **실제 경로**를 nativeOpenFile 에 넘긴다(에셋 추출과 같은 방식).
    // ⚠️ .gltf(분리형)는 .bin·텍스처가 같은 폴더에 있어야 해서 파일 하나만 고르는
    //    선택기로는 잘 안 열린다. 단일 파일인 .glb 를 쓰는 것이 안전하다.
    private fun pickFileToOpen() {
        val i = Intent(Intent.ACTION_OPEN_DOCUMENT).apply {
            addCategory(Intent.CATEGORY_OPENABLE)
            type = "*/*"   // 확장자 MIME 이 표준화돼 있지 않아(.glb/.lot) 전체를 열고 이름으로 판별
        }
        startActivityForResult(i, REQ_OPEN_FILE)
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        super.onActivityResult(requestCode, resultCode, data)
        if (requestCode != REQ_OPEN_FILE || resultCode != RESULT_OK) return
        val uri = data?.data ?: return
        val dst = copyToFiles(uri) ?: return
        if (CadNative.nativeOpenFile(dst.absolutePath)) CadNative.nativeZoomExtents()
    }

    private fun copyToFiles(uri: Uri): File? = try {
        val name = displayName(uri) ?: "imported.bin"
        val dst = File(File(filesDir, "imported").apply { mkdirs() }, name)
        contentResolver.openInputStream(uri)?.use { input ->
            dst.outputStream().use { input.copyTo(it) }
        }
        if (dst.length() > 0) dst else null
    } catch (e: Exception) { null }

    private fun displayName(uri: Uri): String? =
        contentResolver.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME), null, null, null)
            ?.use { c -> if (c.moveToFirst()) c.getString(0) else null }

    // assets/ 하위(models/fonts/textures)를 filesDir 로 재귀 복사.
    private fun extractAssets() {
        for (dir in listOf("models", "fonts", "textures")) copyAssetDir(dir)
    }

    private fun copyAssetDir(dir: String) {
        val items = assets.list(dir) ?: return
        File(filesDir, dir).mkdirs()
        for (item in items) {
            val path = "$dir/$item"
            val children = assets.list(path)
            if (children != null && children.isNotEmpty()) {
                copyAssetDir(path)  // 하위 폴더
            } else {
                assets.open(path).use { input ->
                    File(filesDir, path).outputStream().use { input.copyTo(it) }
                }
            }
        }
    }
}
