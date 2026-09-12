import AppKit

/// 메뉴·리본·도구모음이 **같은 정의**를 쓴다. 한 곳만 고치면 세 군데가 같이 바뀐다.
///
/// 엔진 쪽 ImGui 앱의 툴바(기본·2D·3D)와 메뉴 구성을 그대로 옮겼다. 버튼이 부르는 이름은
/// 엔진 명령행 별칭과 같다(`line`, `rec`, `pp` …) — 명령행에 쳐도 같은 일이 일어난다.
struct CADTool {
    let id: String
    let title: String
    /// SF Symbol 이름. 없는 이름이면 제목 첫 글자를 그려 대신한다(ToolIcons 참고).
    let symbol: String
    let tip: String
    /// 메뉴 단축키. 빈 문자열이면 없음.
    var key: String = ""
    var keyMask: NSEvent.ModifierFlags = [.command]
    /// 아이콘 색. 계열별로 다르게 칠해 한눈에 구분되게 한다(그리기=하늘, 수정=주황 …). nil = 기본.
    var tint: NSColor? = nil
    /// 아이콘 배율. 큐브처럼 글리프가 작게 그려지는 심볼은 1.3 정도 키워야 다른 것과 크기가 맞는다.
    var iconScale: CGFloat = 1.0
    let run: @MainActor (AppDelegate) -> Void
}

struct RibbonGroup {
    let title: String
    let tools: [CADTool]
}

struct RibbonTab {
    let title: String
    let groups: [RibbonGroup]
}

/// 도킹 도구모음 하나. 고전 CAD 처럼 "그리기", "수정" 같은 이름 붙은 세로 띠 하나가 이것이다.
struct ToolbarDefinition {
    let id: String
    let title: String
    let tools: [CADTool]
}

@MainActor
final class ToolCatalog {
    private(set) var byId: [String: CADTool] = [:]

    // 메뉴가 쓰는 묶음
    private(set) var fileTools: [CADTool] = []
    private(set) var editTools: [CADTool] = []
    private(set) var transformTools: [CADTool] = []
    private(set) var gizmoTools: [CADTool] = []
    private(set) var drawTools: [CADTool] = []
    private(set) var annotationTools: [CADTool] = []
    private(set) var modifyTools: [CADTool] = []
    private(set) var polylineTools: [CADTool] = []
    private(set) var primitiveTools: [CADTool] = []
    private(set) var solidOpTools: [CADTool] = []
    private(set) var booleanTools: [CADTool] = []
    private(set) var edgeTools: [CADTool] = []
    private(set) var viewpointTools: [CADTool] = []
    private(set) var cameraTools: [CADTool] = []
    private(set) var layoutTools: [CADTool] = []
    private(set) var styleTools: [CADTool] = []
    private(set) var displayTools: [CADTool] = []
    private(set) var navigateTools: [CADTool] = []
    private(set) var materialTools: [CADTool] = []
    private(set) var uiTools: [CADTool] = []
    private(set) var helpTools: [CADTool] = []

    private(set) var ribbonTabs: [RibbonTab] = []
    private(set) var toolbars: [ToolbarDefinition] = []
    /// 처음 실행(저장된 배치가 없을 때)의 도구모음 배치.
    let defaultToolbarLayout: (left: [String], right: [String]) = (["standard", "draw", "modify"], ["solid", "view"])

    init() {
        // ── 파일 ── (대화상자는 호스트가 띄운다 — 엔진 명령 open/save 는 ImGui 대화상자라 임베드에선 못 쓴다)
        fileTools = [
            api("new",    "새로 만들기",          "doc.badge.plus",          "새 빈 문서(탭)", key: "n") { $0.newDocument() },
            api("open",   "열기 (새 탭)…",        "folder",                  ".lot .obj .stl .dxf .ply .gltf .glb .fbx", key: "o") { $0.openDocumentDialog(asNewTab: true) },
            api("import", "가져오기 (현재 탭)…",  "tray.and.arrow.down",     "현재 문서에 파일 내용을 추가", key: "o", mask: [.command, .shift]) { $0.openDocumentDialog(asNewTab: false) },
            api("save",   "다른 이름으로 저장…",  "square.and.arrow.down.on.square", ".lot(프로젝트) 또는 .stl .dxf .obj .glb 로 내보내기", key: "s") { $0.saveDialog(selectedOnly: false) },
            api("export", "내보내기 (선택)…",     "square.and.arrow.up",     "선택한 객체만 파일로", key: "e") { $0.saveDialog(selectedOnly: true) },
        ]

        // ── 편집 ──
        editTools = [
            api("undo",      "실행 취소", "arrow.uturn.backward", "직전 동작 되돌리기", key: "z") { $0.engine.undo() },
            api("redo",      "다시 실행", "arrow.uturn.forward",  "되돌린 동작 다시 적용", key: "z", mask: [.command, .shift]) { $0.engine.redo() },
            api("selectall", "전체 선택", "selection.pin.in.out", "모든 객체 선택", key: "a") { $0.engine.selectAll() },
            api("deselect",  "선택 해제", "square.dashed",        "선택 비우기", key: "a", mask: [.command, .shift]) { $0.engine.clearSelection() },
            api("delete",    "삭제",      "trash",                "선택 객체 삭제 (뷰에서 Delete 키도 됨)") { $0.engine.deleteSelected() },
            api("clear",     "모두 지움", "xmark.bin",            "씬의 모든 객체 삭제") { $0.engine.clearAll() },
        ]

        // ── 변환 (엔진 명령 — 기준점을 클릭하거나 값을 입력한다) ──
        transformTools = tinted(.systemOrange, [
            cmd("m",   "이동",     "arrow.up.and.down.and.arrow.left.and.right", "기준점 → 목적지 또는 거리 입력"),
            cmd("ro",  "회전",     "arrow.clockwise",                            "중심 → 각도 또는 각도 입력"),
            cmd("sc",  "축척",     "arrow.up.left.and.arrow.down.right",         "기준점 → 배율 또는 배율 입력"),
            cmd("cp",  "복사",     "plus.square.on.square",                      "기준점 → 목적지 (반복 복사)"),
            cmd("ar",  "배열",     "square.grid.3x3",                            "직사각형(행×열) / 원형 배열"),
            cmd("gro", "그룹회전", "arrow.triangle.2.circlepath",                "선택 전체를 지정 축 중심으로 3D 회전"),
        ])
        gizmoTools = tinted(.systemYellow, [
            cmd("gm", "기즈모 이동", "move.3d",   "화살표 핸들을 끌어 이동 (기준점 안 물음)"),
            cmd("gr", "기즈모 회전", "rotate.3d", "링 핸들을 끌어 회전"),
            cmd("gs", "기즈모 축척", "scale.3d",  "상자 핸들을 끌어 축척"),
        ])

        // ── 그리기 ──
        drawTools = tinted(.systemCyan, [
            cmd("line",   "선",       "line.diagonal", "두 점 클릭"),
            cmd("rec",    "사각형",   "rectangle",     "두 대각 모서리"),
            cmd("circle", "원",       "circle",        "중심 + 반지름"),
            cmd("pol",    "정다각형", "hexagon",       "sides 명령으로 변 수 변경"),
            cmd("arc",    "호",       "point.topleft.down.to.point.bottomright.curvepath", "3점 (arcmode 로 모드 순환)"),
            cmd("pl",     "폴리선",   "point.3.connected.trianglepath.dotted", "N클릭, x=완료 c=닫기"),
        ])
        annotationTools = tinted(.systemCyan, [
            cmd("text", "문자", "character.textbox",           "클릭해 배치 후 타이핑, 더블클릭 재편집"),
            cmd("dim",  "치수", "lines.measurement.horizontal", "두 측정점 + 치수선 위치"),
        ])

        // ── 수정 ──
        modifyTools = tinted(.systemOrange, [
            cmd("offset",  "오프셋", "rectangle.on.rectangle",   "선/원/호/폴리선 평행 복제"),
            cmd("mirror",  "대칭",   "flip.horizontal",          "미러선 기준 대칭 복제"),
            cmd("align",   "정렬",   "align.horizontal.left",    "소스·대상 점으로 정렬(이동+회전)"),
            cmd("trim",    "자르기", "scissors",                 "경계까지 잘라내기"),
            cmd("extend",  "연장",   "arrow.right.to.line",      "경계까지 늘리기"),
            cmd("fillet",  "필렛",   "point.bottomleft.forward.to.point.topright.scurvepath", "두 선 모서리 호 처리"),
            cmd("chamfer", "모따기", "triangle.lefthalf.filled", "두 선 모서리 직선 처리"),
            cmd("join",    "결합",   "link",                     "연결된 선/폴리선을 하나로"),
            api("explode", "분해",   "burst",                    "문자→윤곽선, 폴리선/사각형→개별 선분") { $0.engine.explode() },
        ])
        polylineTools = tinted(.systemOrange, [
            cmd("pclose",   "닫기",      "lock",                   "마지막 점 ↔ 첫 점 연결"),
            cmd("popen",    "열기",      "lock.open",              "닫힌 폴리선의 마지막 연결 해제"),
            cmd("preverse", "방향 반전", "arrow.left.arrow.right", "정점 순서 뒤집기"),
            cmd("pinsert",  "정점 삽입", "plus.circle",            "선택 그립 뒤에 중점 추가"),
            cmd("pdelete",  "정점 삭제", "minus.circle",           "선택한 그립의 정점 제거"),
        ])

        // ── 솔리드 ──
        primitiveTools = tinted(.systemGreen, [
            cmd("box",    "박스",   "cube",                "3단계 스케치", scale: 1.35),
            cmd("sphere", "구",     "circle.fill",         "기본 크기로 생성 후 이동/축척"),
            cmd("cyl",    "원기둥", "cylinder",            "축=Z, 기본 크기"),
            cmd("cone",   "원뿔",   "cone",                "축=Z, 기본 크기"),
            cmd("torus",  "토러스", "torus",               "평면=XY"),
            cmd("arrow",  "화살표", "location.north.line", "꼬리/촉 2점 (방향 표시)"),
        ])
        solidOpTools = tinted(.systemGreen, [
            cmd("x",     "돌출",   "square.stack.3d.up",         "닫힌 스케치 → 3D 솔리드"),
            cmd("rev",   "회전체", "rotate.3d.circle",           "닫힌 단면 + 축 → 360° 회전 솔리드"),
            cmd("sw",    "스윕",   "wind",                       "닫힌 단면 + 경로 → 쓸기 솔리드"),
            api("loft",  "로프트", "square.stack.3d.down.right", "선택한 닫힌 단면 2개 이상을 이어 솔리드") { $0.engine.loft() },
            api("shell", "셸",     "shippingbox",                "선택 솔리드 속 비우기 (두께 자동)") { $0.engine.shell() },
            cmd("pp",    "밀당",   "arrow.up.and.down.square",   "면 클릭 → 드래그/거리로 밀기·당기기"),
        ])
        booleanTools = tinted(.systemGreen, [
            api("union",     "합집합", "square.on.square",                     "선택 메시 2개+ 합치기") { $0.engine.booleanUnion() },
            api("intersect", "교집합", "square.on.square.intersection.dashed", "선택 메시 2개+ 교집합") { $0.engine.booleanIntersection() },
        ])
        edgeTools = tinted(.systemGreen, [
            api("ef", "모서리 필렛",   "rectangle.roundedtop",          "솔리드 볼록 모서리 둥글리기 (거리 자동)") { $0.engine.edgeFillet() },
            api("ec", "모서리 모따기", "rectangle.tophalf.inset.filled", "솔리드 볼록 모서리 평면 모따기 (거리 자동)") { $0.engine.edgeChamfer() },
        ])

        // ── 뷰 ──
        viewpointTools = tinted(.systemPurple, [
            api("view_front", "정면도",    "1.square", "정면에서 보기 (뷰에서 1 키)", key: "1") { $0.engine.setView(.front) },
            api("view_top",   "평면도",    "2.square", "위에서 보기 (2 키)",          key: "2") { $0.engine.setView(.top) },
            api("view_right", "우측면도",  "3.square", "오른쪽에서 보기 (3 키)",      key: "3") { $0.engine.setView(.right) },
            api("view_iso",   "Isometric", "4.square", "등각 투영 (4 키)",            key: "4") { $0.engine.setView(.isometric) },
        ])
        cameraTools = tinted(.systemPurple, [
            api("z",     "전체 보기", "arrow.up.left.and.down.right.magnifyingglass", "모든 객체를 화면에 맞춤", key: "0") { $0.engine.zoomExtents() },
            api("focus", "선택 맞춤", "scope",       "선택 객체에 카메라 포커스") { $0.engine.focusSelected() },
            api("proj",  "투영 전환", "perspective", "원근 ↔ 직교") { $0.engine.toggleProjection() },
        ])
        layoutTools = tinted(.systemPurple, [
            api("layout1", "1분할 (단일)", "rectangle",           "뷰포트 하나") { $0.engine.setViewportLayout(0) },
            api("layout2", "2분할 (좌우)", "rectangle.split.2x1", "뷰포트 둘")   { $0.engine.setViewportLayout(1) },
            api("layout3", "3분할",        "rectangle.split.3x1", "뷰포트 셋")   { $0.engine.setViewportLayout(2) },
            api("layout4", "4분할",        "rectangle.split.2x2", "뷰포트 넷")   { $0.engine.setViewportLayout(3) },
        ])
        styleTools = tinted(.systemPurple, [
            api("style0", "음영",          "cube.fill",                     "Shaded",        scale: 1.35) { $0.setVisualStyle(0) },
            api("style1", "음영 + 모서리", "cube",                          "Shaded + Edge", scale: 1.35) { $0.setVisualStyle(1) },
            api("style2", "와이어프레임",  "cube.transparent",              "Wireframe",     scale: 1.35) { $0.setVisualStyle(2) },
            api("style4", "숨은선 제거",   "square.3.layers.3d.down.right", "Hidden Line")   { $0.setVisualStyle(4) },
        ])
        displayTools = tinted(.systemPurple, [
            api("grid",    "격자", "grid",                  "바닥 격자 표시") { $0.toggleGrid() },
            cmd("section", "단면", "square.split.diagonal", "평면/슬라이스/상자로 잘라 내부 보기"),
        ])

        // ── 탐색 모드 (엔진의 캐드/비행/걷기/조종) ──
        navigateTools = tinted(.systemTeal, [
            cmd("cad",       "캐드",   "cursorarrow.click", "그리기/편집 — 궤도 카메라 + 명령행"),
            cmd("fly",       "비행",   "airplane",          "자유 비행 — WASD 이동, 마우스 시선, ESC 복귀"),
            cmd("walk",      "걷기",   "figure.walk",       "눈높이 워크스루 — 바닥 따라 걷기"),
            cmd("character", "조종",   "gamecontroller",    "3인칭 캐릭터 — 선택 객체를 WASD 로 이동"),
            cmd("drive",     "자동차", "car",               "W/S 전·후진, A/D 조향 (다시 누르면 캐릭터로)"),
        ])

        // ── 재질 ──
        materialTools = tinted(.systemPink, [
            api("material", "다음 재질",     "paintpalette",          "선택 객체에 다음 재질 적용") { $0.engine.cycleMaterial() },
            api("matoff",   "재질 제거",     "eraser",                "선택 객체 재질 제거") { $0.engine.removeMaterial() },
            api("tex_up",   "텍스처 배율 +", "plus.magnifyingglass",  "텍스처 타일 확대") { $0.engine.adjustTextureScale(0.5) },
            api("tex_down", "텍스처 배율 -", "minus.magnifyingglass", "텍스처 타일 축소") { $0.engine.adjustTextureScale(-0.5) },
            api("light",    "광원 배치",     "lightbulb",             "클릭한 자리에 광원") { $0.engine.startLightPlacement() },
        ])

        // ── 창 구성 ──
        uiTools = [
            api("ui_ribbon",     "리본 표시",              "rectangle.topthird.inset.filled",  "리본 켜고 끄기", key: "r", mask: [.command, .option]) { $0.toggleRibbon() },
            api("tb_toggle_all", "도구모음 모두 표시/숨김", "sidebar.left",                     "도구모음 전체를 한꺼번에", key: "t", mask: [.command, .option]) { $0.toggleAllToolbars() },
            api("tb_all_left",   "모두 왼쪽에 도킹",       "rectangle.lefthalf.inset.filled",  "보이는 도구모음을 전부 왼쪽으로") { $0.moveAllToolbars(to: .left) },
            api("tb_all_right",  "모두 오른쪽에 도킹",     "rectangle.righthalf.inset.filled", "보이는 도구모음을 전부 오른쪽으로") { $0.moveAllToolbars(to: .right) },
            api("tb_reset",      "도구모음 배치 초기화",   "arrow.counterclockwise",           "처음 배치로 되돌린다") { $0.resetToolbarLayout() },
        ]

        // ── 도움말 ──
        helpTools = [
            api("help_cmdline", "명령행 사용법",           "keyboard",              "하단 명령행에 치는 법") { $0.showCommandLineHelp() },
            api("help_cmds",    "플러그인 명령 보기",      "list.bullet",           "플러그인이 등록한 명령 목록") { $0.showRegisteredCommands() },
            api("help_plugins", "플러그인 폴더 불러오기…", "puzzlepiece.extension", "폴더의 플러그인을 올리고 메뉴·리본에 붙인다") { $0.loadPluginsDialog() },
        ]

        // ── 리본 탭 (엔진 툴바의 기본 / 2D / 3D 를 탭으로) ──
        ribbonTabs = [
            RibbonTab(title: "홈", groups: [
                RibbonGroup(title: "파일",   tools: pick("new", "open", "save")),
                RibbonGroup(title: "편집",   tools: pick("undo", "redo", "delete", "selectall", "clear")),
                RibbonGroup(title: "변환",   tools: pick("m", "ro", "sc", "cp", "ar")),
                RibbonGroup(title: "기즈모", tools: gizmoTools),
                RibbonGroup(title: "화면",   tools: pick("z", "focus")),
            ]),
            RibbonTab(title: "그리기", groups: [
                RibbonGroup(title: "2D",   tools: drawTools),
                RibbonGroup(title: "주석", tools: annotationTools),
            ]),
            RibbonTab(title: "수정", groups: [
                RibbonGroup(title: "편집",   tools: modifyTools),
                RibbonGroup(title: "폴리선", tools: polylineTools),
            ]),
            RibbonTab(title: "솔리드", groups: [
                RibbonGroup(title: "기본체", tools: primitiveTools),
                RibbonGroup(title: "생성",   tools: solidOpTools),
                RibbonGroup(title: "불리언", tools: booleanTools),
                RibbonGroup(title: "모서리", tools: edgeTools),
            ]),
            RibbonTab(title: "뷰", groups: [
                RibbonGroup(title: "시점",   tools: viewpointTools),
                RibbonGroup(title: "카메라", tools: cameraTools),
                RibbonGroup(title: "분할",   tools: pick("layout1", "layout2", "layout4")),
                RibbonGroup(title: "표시",   tools: pick("style0", "style2", "style4", "grid", "section")),
                RibbonGroup(title: "탐색",   tools: navigateTools),
                RibbonGroup(title: "재질",   tools: materialTools),
            ]),
        ]

        // ── 도킹 도구모음 (고전 CAD 의 이름 붙은 툴바들 — 각각 따로 좌/우 도킹·숨김) ──
        toolbars = [
            ToolbarDefinition(id: "standard", title: "표준",   tools: pick("new", "open", "save", "undo", "redo", "delete", "selectall")),
            ToolbarDefinition(id: "draw",     title: "그리기", tools: drawTools + annotationTools),
            ToolbarDefinition(id: "modify",   title: "수정",   tools: pick("m", "ro", "sc", "cp", "ar") + modifyTools),
            ToolbarDefinition(id: "solid",    title: "솔리드", tools: primitiveTools + solidOpTools + booleanTools + edgeTools),
            ToolbarDefinition(id: "view",     title: "뷰",     tools: cameraTools + viewpointTools + pick("layout1", "layout4", "style0", "style2", "grid")),
            ToolbarDefinition(id: "navigate", title: "탐색",   tools: navigateTools),
            ToolbarDefinition(id: "gizmo",    title: "기즈모", tools: gizmoTools),
            ToolbarDefinition(id: "material", title: "재질",   tools: materialTools),
        ]
    }

    func pick(_ ids: String...) -> [CADTool] {
        ids.compactMap { byId[$0] }
    }

    // MARK: - 정의 도우미

    /// 엔진 명령행 별칭을 그대로 부르는 도구. id 가 곧 명령 이름이다.
    private func cmd(_ id: String, _ title: String, _ symbol: String, _ tip: String,
                     scale: CGFloat = 1.0) -> CADTool {
        register(CADTool(id: id, title: title, symbol: symbol, tip: tip, iconScale: scale) { app in
            app.engine.execute(id)
        })
    }

    /// C API 를 직접 부르거나 호스트 UI(대화상자 등)를 여는 도구.
    private func api(_ id: String, _ title: String, _ symbol: String, _ tip: String,
                     key: String = "", mask: NSEvent.ModifierFlags = [.command], scale: CGFloat = 1.0,
                     _ run: @escaping @MainActor (AppDelegate) -> Void) -> CADTool {
        register(CADTool(id: id, title: title, symbol: symbol, tip: tip, key: key, keyMask: mask,
                         iconScale: scale, run: run))
    }

    /// 계열 색을 입힌다. byId 도 같이 갱신해 pick() 으로 꺼내도 색이 붙어 있게.
    private func tinted(_ color: NSColor, _ tools: [CADTool]) -> [CADTool] {
        tools.map { tool in
            var t = tool
            t.tint = color
            byId[t.id] = t
            return t
        }
    }

    private func register(_ tool: CADTool) -> CADTool {
        byId[tool.id] = tool
        return tool
    }
}

/// 버튼 아이콘. SF Symbol 이 있으면 그걸, 없으면(플러그인 항목·구버전 OS) 글자를 그린다.
@MainActor
enum ToolIcons {
    static func image(symbol: String, fallback: String, pointSize: CGFloat) -> NSImage {
        if !symbol.isEmpty,
           let img = NSImage(systemSymbolName: symbol, accessibilityDescription: fallback) {
            let config = NSImage.SymbolConfiguration(pointSize: pointSize, weight: .regular)
            return img.withSymbolConfiguration(config) ?? img
        }
        return textBadge(String(fallback.prefix(2)), pointSize: pointSize)
    }

    /// 버튼에 아이콘과 색을 한 번에. 심볼은 템플릿 이미지라 contentTintColor 로 칠해진다.
    /// 크기는 기준값 × 도구별 배율 — 큐브처럼 작게 그려지는 글리프를 여기서 보정한다.
    static func apply(_ tool: CADTool, to button: NSButton, pointSize: CGFloat) {
        button.image = image(symbol: tool.symbol, fallback: tool.title, pointSize: pointSize * tool.iconScale)
        button.contentTintColor = tool.tint
    }

    private static func textBadge(_ text: String, pointSize: CGFloat) -> NSImage {
        let side = pointSize * 1.5
        let image = NSImage(size: NSSize(width: side, height: side), flipped: false) { rect in
            let path = NSBezierPath(roundedRect: rect.insetBy(dx: 1, dy: 1), xRadius: 5, yRadius: 5)
            NSColor.secondaryLabelColor.withAlphaComponent(0.35).setFill()
            path.fill()
            let attrs: [NSAttributedString.Key: Any] = [
                .font: NSFont.systemFont(ofSize: pointSize * 0.7, weight: .semibold),
                .foregroundColor: NSColor.labelColor,
            ]
            let str = NSAttributedString(string: text, attributes: attrs)
            let size = str.size()
            str.draw(at: NSPoint(x: (rect.width - size.width) / 2, y: (rect.height - size.height) / 2))
            return true
        }
        image.isTemplate = false
        return image
    }
}
