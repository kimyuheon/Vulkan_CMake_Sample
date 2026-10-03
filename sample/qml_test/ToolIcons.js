.pragma library

// 엔진 ui/lot_tool_icons.cpp 를 Canvas 2D 로 옮긴 것 — 같은 도형, 같은 치수.
//
// 외부 이미지·아이콘 폰트를 쓰지 않는 이유도 엔진과 같다: DPI 배율과 플랫폼이 달라도 똑같이 보이고
// 색은 테마 글자색을 그대로 따른다. 설계 반경은 ≈7px, scale 로 키운다(작은 칸 1.0, 큰 버튼 1.9).
//
// ImGui → Canvas 대응: AddLine/AddRect → stroke, AddCircleFilled → arc+fill,
// PathArcTo+PathStroke → arc (둘 다 0 = +X, 각도 증가 = 화면상 시계방향이라 각도를 그대로 쓴다).

var TAU = 6.28318530718;

// 배율이 적용된 좌표·두께 도우미 — 엔진 Pen 과 같은 역할.
function Pen(ctx, cx, cy, color, k) {
    this.d = ctx; this.cx = cx; this.cy = cy; this.col = color; this.k = k;
}
Pen.prototype.px = function (x) { return this.cx + x * this.k; };
Pen.prototype.py = function (y) { return this.cy + y * this.k; };
Pen.prototype.r  = function (v) { return v * this.k; };
// 1px 아래로는 안 내려간다 — 가늘어지면 끊겨 보인다(엔진과 같은 규칙).
Pen.prototype.t  = function (v) { return v * this.k < 1.0 ? 1.0 : v * this.k; };

function line(q, x1, y1, x2, y2, th) {
    var c = q.d;
    c.beginPath(); c.lineWidth = q.t(th === undefined ? 1.0 : th); c.strokeStyle = q.col;
    c.moveTo(q.px(x1), q.py(y1)); c.lineTo(q.px(x2), q.py(y2)); c.stroke();
}

// 모서리 둥근 사각형 경로 — rounding 은 이미 배율이 적용된 값(엔진이 q.r() 로 넘긴다).
function rectPath(q, x1, y1, x2, y2, rounding) {
    var c = q.d, a = { x: q.px(x1), y: q.py(y1) }, b = { x: q.px(x2), y: q.py(y2) };
    var rr = Math.min(rounding || 0, Math.abs(b.x - a.x) / 2, Math.abs(b.y - a.y) / 2);
    c.beginPath();
    if (rr <= 0) { c.rect(a.x, a.y, b.x - a.x, b.y - a.y); return; }
    c.moveTo(a.x + rr, a.y);
    c.lineTo(b.x - rr, a.y); c.quadraticCurveTo(b.x, a.y, b.x, a.y + rr);
    c.lineTo(b.x, b.y - rr); c.quadraticCurveTo(b.x, b.y, b.x - rr, b.y);
    c.lineTo(a.x + rr, b.y); c.quadraticCurveTo(a.x, b.y, a.x, b.y - rr);
    c.lineTo(a.x, a.y + rr); c.quadraticCurveTo(a.x, a.y, a.x + rr, a.y);
    c.closePath();
}
function rect(q, x1, y1, x2, y2, rounding, th) {
    rectPath(q, x1, y1, x2, y2, rounding);
    q.d.lineWidth = q.t(th === undefined ? 1.0 : th); q.d.strokeStyle = q.col; q.d.stroke();
}
function rectFilled(q, x1, y1, x2, y2, rounding) {
    rectPath(q, x1, y1, x2, y2, rounding);
    q.d.fillStyle = q.col; q.d.fill();
}

function circle(q, x, y, radius, th) {
    var c = q.d;
    c.beginPath(); c.lineWidth = q.t(th === undefined ? 1.0 : th); c.strokeStyle = q.col;
    c.arc(q.px(x), q.py(y), q.r(radius), 0, TAU); c.stroke();
}
function circleFilled(q, x, y, radius) {
    var c = q.d;
    c.beginPath(); c.fillStyle = q.col; c.arc(q.px(x), q.py(y), q.r(radius), 0, TAU); c.fill();
}
// 축 정렬 타원 — ImGui AddEllipse(center, radii, rot=0) 과 같다.
function ellipse(q, x, y, rx, ry, th) {
    var c = q.d;
    c.save(); c.translate(q.px(x), q.py(y)); c.scale(1.0, q.r(ry) / q.r(rx));
    c.beginPath(); c.arc(0, 0, q.r(rx), 0, TAU); c.restore();
    c.lineWidth = q.t(th === undefined ? 1.0 : th); c.strokeStyle = q.col; c.stroke();
}

function polyPath(q, pts) {
    var c = q.d;
    c.beginPath(); c.moveTo(q.px(pts[0][0]), q.py(pts[0][1]));
    for (var i = 1; i < pts.length; ++i) c.lineTo(q.px(pts[i][0]), q.py(pts[i][1]));
}
function polyline(q, pts, th) {
    polyPath(q, pts); q.d.lineWidth = q.t(th); q.d.strokeStyle = q.col; q.d.stroke();
}
function triangle(q, pts, th) {
    polyPath(q, pts); q.d.closePath(); q.d.lineWidth = q.t(th); q.d.strokeStyle = q.col; q.d.stroke();
}
function triangleFilled(q, pts) {
    polyPath(q, pts); q.d.closePath(); q.d.fillStyle = q.col; q.d.fill();
}
// 화면 좌표로 직접 받는 삼각형(화살촉용) — 배율이 이미 적용된 점들.
function triFilledAbs(q, p0, p1, p2) {
    var c = q.d;
    c.beginPath(); c.moveTo(p0.x, p0.y); c.lineTo(p1.x, p1.y); c.lineTo(p2.x, p2.y);
    c.closePath(); c.fillStyle = q.col; c.fill();
}

// 화살촉만 — tip 에서 dir(단위벡터) 방향을 보는 삼각형.
function arrowHead(q, tipX, tipY, dx, dy, headLen, headHalfW) {
    var sx = -dy, sy = dx;
    var hl = q.r(headLen), hw = q.r(headHalfW);
    triFilledAbs(q, { x: tipX, y: tipY },
        { x: tipX - dx * hl + sx * hw, y: tipY - dy * hl + sy * hw },
        { x: tipX - dx * hl - sx * hw, y: tipY - dy * hl - sy * hw });
}
// 직선 화살표 — 촉은 끝에만. 좌표는 설계 단위.
function arrow(q, x1, y1, x2, y2, th, headLen, headHalfW) {
    th = th === undefined ? 1.5 : th;
    headLen = headLen === undefined ? 4.0 : headLen;
    headHalfW = headHalfW === undefined ? 2.5 : headHalfW;
    line(q, x1, y1, x2, y2, th);
    var ax = q.px(x1), ay = q.py(y1), bx = q.px(x2), by = q.py(y2);
    var dx = bx - ax, dy = by - ay, len = Math.sqrt(dx * dx + dy * dy);
    if (len < 0.01) return;
    arrowHead(q, bx, by, dx / len, dy / len, headLen, headHalfW);
}
// 원호 끝에 촉이 붙는 회전 화살표. radius 는 **이미 배율이 적용된** 값(엔진과 같다).
function arcArrow(q, x, y, radius, a0, a1, th) {
    var c = q.d, cx = q.px(x), cy = q.py(y);
    c.beginPath(); c.lineWidth = q.t(th); c.strokeStyle = q.col;
    c.arc(cx, cy, radius, a0, a1, a1 < a0); c.stroke();
    var dir = (a1 >= a0) ? 1.0 : -1.0;
    var anX = cx + Math.cos(a1) * radius, anY = cy + Math.sin(a1) * radius;
    var tx = dir * -Math.sin(a1), ty = dir * Math.cos(a1);
    var nx = -ty, ny = tx;
    var hl = q.r(3.3), hw = q.r(1.8);
    var tipX = anX + tx * hl * (2.0 / 3.0), tipY = anY + ty * hl * (2.0 / 3.0);
    var bX = anX - tx * hl * (1.0 / 3.0), bY = anY - ty * hl * (1.0 / 3.0);
    triFilledAbs(q, { x: tipX, y: tipY },
        { x: bX + nx * hw, y: bY + ny * hw }, { x: bX - nx * hw, y: bY - ny * hw });
}

// ── 아이콘 ──────────────────────────────────────────────────────────
// 이름은 엔진 enum 과 같다(Basic/TwoD/ThreeD 에 겹치는 이름이 없어 한 묶음으로 둔다).
function drawIcon(ctx, name, cx, cy, color, scale) {
    var q = new Pen(ctx, cx, cy, color, scale);
    ctx.lineCap = "round"; ctx.lineJoin = "round";

    switch (name) {
    // ── Basic ──
    case "GizmoMove": case "Move":
        // 네 방향 화살표 ✥ — 선 두 줄이 가운데서 한 번만 교차하고 네 끝에 작은 촉.
        line(q, -6.2, 0, 6.2, 0, 1.4); line(q, 0, -6.2, 0, 6.2, 1.4);
        arrowHead(q, q.px(-6.2), q.py(0), -1, 0, 3.0, 1.7);
        arrowHead(q, q.px(6.2), q.py(0), 1, 0, 3.0, 1.7);
        arrowHead(q, q.px(0), q.py(-6.2), 0, -1, 3.0, 1.7);
        arrowHead(q, q.px(0), q.py(6.2), 0, 1, 3.0, 1.7); break;
    case "GizmoRotate": case "Rotate": case "GroupRotate":
        arcArrow(q, 0, 0, q.r(6.0), 0.65, TAU, 1.5); break;
    case "GizmoScale": case "Scale":
        rect(q, -4.5, -4.5, 4.5, 4.5, q.r(1.0), 1.4);
        arrow(q, -1.0, -1.0, -6.0, -6.0); arrow(q, 1.0, 1.0, 6.0, 6.0); break;
    case "Copy":
        rect(q, -5.5, -5.5, 2.5, 2.5, q.r(1.0), 1.3);
        rect(q, -1.5, -1.5, 5.5, 5.5, q.r(1.0), 1.3); break;
    case "Array":
        for (var ay2 = -1; ay2 <= 1; ay2 += 2) for (var ax2 = -1; ax2 <= 1; ax2 += 2)
            rectFilled(q, ax2 * 3.8 - 1.4, ay2 * 3.8 - 1.4, ax2 * 3.8 + 1.4, ay2 * 3.8 + 1.4, 0);
        break;
    case "Delete": case "ClearAll":
        rect(q, -4.0, -3.5, 4.0, 5.5, q.r(1.0), 1.3);
        line(q, -5.5, -5.5, 5.5, -5.5, 1.5); line(q, -1.8, -7.0, 1.8, -7.0, 1.5); break;
    case "SelectAll":
        rect(q, -5.0, -5.0, 5.0, 5.0, 0, 1.3);
        circleFilled(q, -7.0, -7.0, 1.2); circleFilled(q, 7.0, 7.0, 1.2); break;
    case "Undo":
        // 회전 아이콘과 같은 원호+촉으로 그려 한 벌로 보이게 — 5/8 바퀴라 "회전"과 구분된다.
        arcArrow(q, 0, 1.0, q.r(5.5), 0.35, -(TAU * 0.5 + 0.5), 1.5); break;
    case "Redo":
        arcArrow(q, 0, 1.0, q.r(5.5), TAU * 0.5 - 0.35, TAU + 0.5, 1.5); break;
    case "ZoomExtents":
        circle(q, -1.5, -1.5, 4.0, 1.5); line(q, 1.5, 1.5, 6.0, 6.0, 1.8); break;
    case "Focus":
        circle(q, 0, 0, 4.8, 1.3); circleFilled(q, 0, 0, 1.4);
        line(q, -7.0, 0, -4.8, 0, 1.3); line(q, 7.0, 0, 4.8, 0, 1.3); break;
    case "Section":
        rect(q, -5.5, -5.0, 5.5, 5.0, q.r(1.0), 1.2); line(q, -6.5, 0, 6.5, 0, 1.8); break;
    case "Cad":
        circle(q, 0, 0, 5.5, 1.3); triangleFilled(q, [[0, -6.0], [-2.5, 2.5], [2.5, 2.5]]); break;
    case "Fly":
        triangleFilled(q, [[-6.5, 2.5], [6.5, 0], [-5.0, -3.5]]); line(q, -1.0, 0, -5.5, 6.0, 1.3); break;
    case "Walk":
        circleFilled(q, 0, -5.0, 2.2); line(q, 0, -2.5, 0, 2.0, 1.5);
        line(q, 0, 2.0, -4.0, 6.0, 1.5); line(q, 0, 2.0, 4.0, 6.0, 1.5); break;
    case "Character":
        circle(q, 0, -4.5, 2.4, 1.3); rect(q, -4.5, -1.5, 4.5, 6.0, q.r(2.0), 1.3); break;
    case "Vehicle":
        rect(q, -6.5, -1.5, 6.5, 4.5, q.r(1.5), 1.3); line(q, -3.5, -1.5, -1.5, -5.0, 1.3);
        line(q, -1.5, -5.0, 3.5, -5.0, 1.3); line(q, 3.5, -5.0, 5.0, -1.5, 1.3); break;

    // ── TwoD ──
    case "Line":      line(q, -7, 6, 7, -6, 1.7); break;
    case "Rectangle": rect(q, -6, -5, 6, 5, q.r(1.2), 1.0); break;
    case "Circle":    circle(q, 0, 0, 6.0, 1.3); break;
    case "Polygon":
        polyline(q, [[0, -6], [5.5, -2.0], [3.5, 5.5], [-3.5, 5.5], [-5.5, -2.0], [0, -6]], 1.3); break;
    case "Arc":
        arcStroke(q, 0, 1, q.r(6.0), 3.65, 6.0, 1.4);
        circleFilled(q, -5.2, -2.0, 1.2); circleFilled(q, 5.8, -0.7, 1.2); break;
    case "Polyline":
        polyline(q, [[-7, 4], [-2.5, -4], [2.0, 3], [7, -3]], 1.5); break;
    case "Text":
        line(q, -6, -6, 6, -6, 1.5); line(q, 0, -6, 0, 6, 1.5); line(q, -3, 6, 3, 6, 1.5); break;
    case "Dimension":
        line(q, -6, -4, -6, 5, 1.2); line(q, 6, -4, 6, 5, 1.2);
        arrow(q, -5.5, 1, 5.5, 1, 1.2); arrow(q, 5.5, 1, -5.5, 1, 1.2); break;
    case "Offset":
        line(q, -6, -4, 4, -4, 1.3); line(q, -4, 4, 6, 4, 1.3); arrow(q, -1, -1, -1, 2, 1.2); break;
    case "Mirror":
        line(q, 0, -7, 0, 7, 1.2);
        triangle(q, [[-6, 4], [-2, 0], [-6, -4]], 1.2); triangle(q, [[6, 4], [2, 0], [6, -4]], 1.2); break;
    case "Align":
        line(q, -6, -4, -1, -4, 1.5); line(q, 1, 4, 6, 4, 1.5); arrow(q, -1, -2, 2, 2, 1.3); break;
    case "Trim":
        line(q, -7, 0, 7, 0, 1.3); line(q, 0, -6, 0, -1, 1.5); line(q, -3, 2, 3, 5, 1.8); break;
    case "Extend":
        line(q, -6, 3, 2, -3, 1.5); line(q, 2, -3, 6, -6, 1.2); arrow(q, 1, -2, 6, -6, 1.2); break;
    case "Fillet":
        line(q, -6, 5, -6, 0, 1.4); line(q, -6, 5, 1, 5, 1.4);
        arcStroke(q, 0, 0, q.r(5.0), 3.14, 4.72, 1.5); break;
    case "Chamfer":
        line(q, -6, 5, -6, -2, 1.4); line(q, -6, 5, 1, 5, 1.4); line(q, -6, -2, 1, 5, 1.4); break;
    case "Join":
        circle(q, -3.5, 0, 3.2, 1.3); circle(q, 3.5, 0, 3.2, 1.3); line(q, -1.5, 0, 1.5, 0, 1.8); break;
    case "Explode":
        rect(q, -6, -5, -1, 0, 0, 1.2); rect(q, 2, -3, 6, 1, 0, 1.2); rect(q, -2, 3, 3, 6, 0, 1.2); break;
    case "PolyClose":
        circle(q, 0, 0, 5.8, 1.3); line(q, -2, 0, 2, 0, 1.4); break;
    case "PolyOpen":
        arcStroke(q, 0, 0, q.r(5.8), 0.7, 5.55, 1.3); break;
    case "PolyReverse":
        arrow(q, -6, -3, 5, -3, 1.2); arrow(q, 6, 3, -5, 3, 1.2); break;
    case "PolyInsert":
        line(q, -6, 0, 6, 0, 1.4); line(q, 0, -5, 0, 5, 1.4); break;
    case "PolyDelete":
        line(q, -5, -5, 5, 5, 1.8); line(q, 5, -5, -5, 5, 1.8); circle(q, 0, 0, 5.5, 1.1); break;

    // ── ThreeD ──
    case "Box":
        polyline(q, [[-5, -3], [1, -6], [6, -3], [0, 0], [-5, -3]], 1.2);
        polyline(q, [[-5, -3], [-5, 4], [0, 7], [0, 0]], 1.2);
        polyline(q, [[6, -3], [6, 4], [0, 7]], 1.2); break;
    case "Sphere":
        circle(q, 0, 0, 6.5, 1.2); ellipse(q, 0, 0, 6.2, 2.5, 1.0); break;
    case "Cylinder":
        ellipse(q, 0, -5, 5.5, 2.2, 1.2); line(q, -5.5, -5, -5.5, 5, 1.2);
        line(q, 5.5, -5, 5.5, 5, 1.2); ellipse(q, 0, 5, 5.5, 2.2, 1.2); break;
    case "Cone":
        line(q, -5.5, 5, 0, -6, 1.3); line(q, 0, -6, 5.5, 5, 1.3); ellipse(q, 0, 5, 5.5, 2.0, 1.2); break;
    case "Torus":
        ellipse(q, 0, 0, 7, 4.5, 1.4); ellipse(q, 0, 0, 3.5, 1.8, 1.3); break;
    case "Arrow":
        arrow(q, -7, 5, 6, -5, 1.8); break;
    case "Extrude":
        rect(q, -5, 1, 5, 6, q.r(1.0), 1.2); arrow(q, 0, 4, 0, -6, 1.4); break;
    case "Revolve":
        line(q, 0, -6, 0, 6, 1.2); arcArrow(q, 0, 0, q.r(6.0), 0.65, TAU, 1.3); break;
    case "Sweep":
        circle(q, -5, 4, 2.2, 1.2);
        ctx.beginPath(); ctx.lineWidth = q.t(1.4); ctx.strokeStyle = color;
        ctx.moveTo(q.px(-3), q.py(3));
        ctx.bezierCurveTo(q.px(-1), q.py(-5), q.px(3), q.py(5), q.px(6), q.py(-3));
        ctx.stroke(); break;
    case "Loft":
        ellipse(q, 0, -5, 3.0, 1.5, 1.2); ellipse(q, 0, 5, 6.0, 2.1, 1.2);
        line(q, -3, -5, -6, 5, 1.2); line(q, 3, -5, 6, 5, 1.2); break;
    case "Shell":
        rect(q, -6, -6, 6, 6, q.r(1.0), 1.3); rect(q, -3, -3, 3, 3, q.r(1.0), 1.2); break;
    case "PushPull":
        rect(q, -5, -4, 5, 4, 0, 1.2); arrow(q, 0, 0, 0, -7, 1.4); arrow(q, 0, 0, 0, 7, 1.4); break;
    case "FaceInset":
        rect(q, -6, -6, 6, 6, 0, 1.2); rect(q, -3, -3, 3, 3, 0, 1.4);
        arrow(q, -6, 0, -3, 0, 1.0, 2.8, 1.8); break;
    case "Union":
        circle(q, -3, 0, 4.5, 1.2); circle(q, 3, 0, 4.5, 1.2); line(q, 0, -2.5, 0, 2.5, 1.2); break;
    case "Difference":
        circle(q, -3, 0, 4.5, 1.2); circle(q, 3, 0, 4.5, 1.2); line(q, 0.5, 0, 5.5, 0, 1.8); break;
    case "Intersect":
        circle(q, -3, 0, 4.5, 1.2); circle(q, 3, 0, 4.5, 1.2); circleFilled(q, 0, 0, 1.8); break;
    case "Slice":
        rect(q, -6, -5, 6, 5, 0, 1.2); line(q, -7, 4, 7, -4, 1.8); break;
    case "EdgeChamfer":
        rect(q, -6, -5, 6, 5, 0, 1.2); line(q, -6, 5, 0, -1, 1.6); break;
    case "EdgeFillet":
        line(q, -6, 5, -6, -2, 1.3); line(q, -6, 5, 1, 5, 1.3);
        arcStroke(q, 0, 0, q.r(5.0), 3.14, 4.72, 1.6); break;
    case "Exploded":   // 가운데 부품 + 위아래로 빠지는 조각과 화살표
        rect(q, -4, -2, 4, 2, 0, 1.3);
        rect(q, -3, -7.5, 3, -5.5, 0, 1.2); rect(q, -3, 5.5, 3, 7.5, 0, 1.2);
        arrow(q, 6.5, -1, 6.5, -7, 1.0, 2.4, 1.6); arrow(q, 6.5, 1, 6.5, 7, 1.0, 2.4, 1.6); break;
    case "Balloon":    // 번호 원 + 지시선 + 점
        circle(q, 2.5, -2.5, 4.5, 1.3); line(q, -0.7, 0.7, -6, 6, 1.2);
        circleFilled(q, -6, 6, 1.3); line(q, 2.5, -4.5, 2.5, -0.5, 1.2); break;
    case "Bom":        // 표
        rect(q, -6.5, -6, 6.5, 6, 0, 1.2);
        line(q, -6.5, -2.0, 6.5, -2.0, 1.0); line(q, -6.5, 2.0, 6.5, 2.0, 1.0);
        line(q, -3, -6, -3, 6, 1.0); break;
    case "Manual":     // 접힌 모서리 종이 + 줄
        polyline(q, [[-5, -7], [2, -7], [5, -4], [5, 7], [-5, 7], [-5, -7]], 1.2);
        line(q, -3, -2, 3, -2, 1.0); line(q, -3, 1, 3, 1, 1.0); line(q, -3, 4, 3, 4, 1.0); break;
    }
}

// 촉 없는 원호 — PathArcTo + PathStroke. radius 는 이미 배율 적용된 값.
function arcStroke(q, x, y, radius, a0, a1, th) {
    var c = q.d;
    c.beginPath(); c.lineWidth = q.t(th); c.strokeStyle = q.col;
    c.arc(q.px(x), q.py(y), radius, a0, a1, a1 < a0); c.stroke();
}
