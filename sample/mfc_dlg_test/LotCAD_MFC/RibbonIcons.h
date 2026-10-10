#pragma once
// 리본 아이콘 — 비트맵 리소스 없이 GDI+ 선으로 그린다(DPI 가 바뀌어도 선명하다).
// 엔진 리본(ImGui)의 아이콘과 같은 모양을 흉내 낸다.

namespace Gdiplus { class Graphics; }

enum class RibbonIcon
{
	None,
	// 홈
	GizmoMove, GizmoRotate, GizmoScale,
	Move, Rotate, Scale, Copy, Array,
	SelectAll, Delete, ClearAll,
	Undo, Redo,
	ZoomExtents, Focus, Projection, ViewIso, ViewTop, ViewFront, ViewRight,
	// 2D
	Line, Rectangle, Circle, Polygon, Arc, Polyline, Text, Dimension,
	Offset, Mirror, Trim, Extend, Fillet, Chamfer,
	// 3D
	Box, Sphere, Cylinder, Cone, Torus,
	Extrude, Revolve, Sweep, Loft, Shell,
	Union, Difference, Intersect, Slice,
	// 도구
	Section, Navigation, Particles,
};

// (cx, cy) 를 중심으로 반지름 r 인 정사각형 안에 그린다. color = 선 색.
void DrawRibbonIcon(Gdiplus::Graphics& g, RibbonIcon icon, float cx, float cy, float r, COLORREF color);
