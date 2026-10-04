// RibbonIcons.cpp: 리본 아이콘 그리기 (GDI+ 벡터)
//

#include "pch.h"
#include "RibbonIcons.h"
#include "DarkTheme.h"

#include <gdiplus.h>
#include <cmath>
#include <initializer_list>
#include <vector>

using namespace Gdiplus;

namespace {
	Color ToColor(COLORREF c, BYTE alpha = 255)
	{
		return Color(alpha, GetRValue(c), GetGValue(c), GetBValue(c));
	}

	// 아이콘 좌표계: 중심 (0,0), 가장자리 ±1, y 는 아래로.
	// 단위 좌표 → 픽셀 변환과 펜·채움 붓을 한데 묶어, 아이콘 하나가 몇 줄로 끝나게 한다.
	struct Painter
	{
		Graphics&  g;
		float      cx, cy, r;
		Pen        pen;
		SolidBrush fill;   // 면 강조 — 강조색 반투명
		SolidBrush dot;    // 끝점 표시 — 선 색

		Painter(Graphics& g_, float cx_, float cy_, float r_, COLORREF color)
			: g(g_), cx(cx_), cy(cy_), r(r_),
			  pen(ToColor(color), (std::max)(1.2f, r_ * 0.15f)),
			  fill(ToColor(DarkTheme::Accent, 110)),
			  dot(ToColor(color))
		{
			pen.SetLineJoin(LineJoinRound);
			pen.SetStartCap(LineCapRound);
			pen.SetEndCap(LineCapRound);
			pen.SetDashCap(DashCapRound);
		}

		PointF P(float x, float y) const { return PointF(cx + x * r, cy + y * r); }
		RectF  R(float x, float y, float w, float h) const { return RectF(cx + x * r, cy + y * r, w * r, h * r); }
		RectF  E(float x, float y, float rx, float ry) const { return R(x - rx, y - ry, rx * 2, ry * 2); }

		void SetColor(COLORREF c) { pen.SetColor(ToColor(c)); dot.SetColor(ToColor(c)); }
		void Dashed(bool on)      { pen.SetDashStyle(on ? DashStyleDash : DashStyleSolid); }

		void Line(float x1, float y1, float x2, float y2) { g.DrawLine(&pen, P(x1, y1), P(x2, y2)); }
		void Rect(float x, float y, float w, float h)     { g.DrawRectangle(&pen, R(x, y, w, h)); }
		void Ellipse(float x, float y, float rx, float ry){ g.DrawEllipse(&pen, E(x, y, rx, ry)); }
		void FillEllipse(float x, float y, float rx, float ry) { g.FillEllipse(&fill, E(x, y, rx, ry)); }
		// start·sweep 은 도(°), 0° = +x, 시계 방향(화면 y 가 아래라서).
		void Arc(float x, float y, float rad, float start, float sweep)
		{
			g.DrawArc(&pen, E(x, y, rad, rad), start, sweep);
		}
		void EllipseArc(float x, float y, float rx, float ry, float start, float sweep)
		{
			g.DrawArc(&pen, E(x, y, rx, ry), start, sweep);
		}
		void Dot(float x, float y)
		{
			const float d = (std::max)(1.5f, r * 0.14f);
			g.FillEllipse(&dot, cx + x * r - d, cy + y * r - d, d * 2, d * 2);
		}

		std::vector<PointF> Map(std::initializer_list<PointF> pts) const
		{
			std::vector<PointF> out;
			for (const PointF& p : pts) out.push_back(P(p.X, p.Y));
			return out;
		}
		void Poly(std::initializer_list<PointF> pts, bool closed = false)
		{
			auto v = Map(pts);
			if (closed) g.DrawPolygon(&pen, v.data(), (INT)v.size());
			else        g.DrawLines(&pen, v.data(), (INT)v.size());
		}
		void FillPoly(std::initializer_list<PointF> pts)
		{
			auto v = Map(pts);
			g.FillPolygon(&fill, v.data(), (INT)v.size());
		}

		// (x,y) 가 촉 끝, (dx,dy) 가 가리키는 방향인 화살촉.
		void Arrow(float x, float y, float dx, float dy)
		{
			const float len = std::sqrt(dx * dx + dy * dy);
			dx /= len; dy /= len;
			const float a = 0.38f, c = 0.866f, s = 0.5f;   // 길이, cos/sin 30°
			Line(x, y, x + a * (-dx * c + dy * s), y + a * (-dy * c - dx * s));
			Line(x, y, x + a * (-dx * c - dy * s), y + a * (-dy * c + dx * s));
		}

		// 등각 정육면체. face: 0=없음 1=윗면 2=앞(왼쪽)면 3=오른쪽면 을 강조색으로 채운다.
		void IsoCube(int face = 0)
		{
			const PointF top(0, -0.9f), ur(0.8f, -0.45f), lr(0.8f, 0.45f),
			             bot(0, 0.9f),  ll(-0.8f, 0.45f), ul(-0.8f, -0.45f), mid(0, 0);
			if (face == 1) FillPoly({ top, ur, mid, ul });
			if (face == 2) FillPoly({ ul, mid, bot, ll });
			if (face == 3) FillPoly({ mid, ur, lr, bot });
			Poly({ top, ur, lr, bot, ll, ul }, true);
			Poly({ ul, mid, ur });
			Line(0, 0, 0, 0.9f);
		}

		// 두 원의 불리언 — op: 0=합 1=차(왼쪽−오른쪽) 2=교. 결과 영역만 채운다.
		void BooleanCircles(int op)
		{
			GraphicsPath a, b;
			a.AddEllipse(E(-0.32f, 0, 0.6f, 0.6f));
			b.AddEllipse(E( 0.32f, 0, 0.6f, 0.6f));
			Region area(&a);
			if (op == 0) area.Union(&b);
			if (op == 1) area.Exclude(&b);
			if (op == 2) area.Intersect(&b);
			g.FillRegion(&fill, &area);
			Ellipse(-0.32f, 0, 0.6f, 0.6f);
			if (op == 1) Dashed(true);
			Ellipse(0.32f, 0, 0.6f, 0.6f);
			Dashed(false);
		}

		// 기즈모 세 축 (X 오른쪽 · Y 안쪽 대각 · Z 위). end: 0=화살촉 1=상자.
		void GizmoAxes(int end)
		{
			const float ox = -0.55f, oy = 0.55f;
			struct Axis { COLORREF color; float x, y; } axes[] = {
				{ DarkTheme::AxisX,  0.9f,   0.55f },
				{ DarkTheme::AxisY,  0.3f,  -0.3f  },
				{ DarkTheme::AxisZ, -0.55f, -0.9f  },
			};
			for (const Axis& a : axes) {
				SetColor(a.color);
				Line(ox, oy, a.x, a.y);
				if (end == 0) Arrow(a.x, a.y, a.x - ox, a.y - oy);
				else          g.FillRectangle(&dot, E(a.x, a.y, 0.16f, 0.16f));
			}
		}
	};
}

void DrawRibbonIcon(Graphics& g, RibbonIcon icon, float cx, float cy, float r, COLORREF color)
{
	Painter p(g, cx, cy, r, color);

	switch (icon)
	{
	// ── 홈 ──────────────────────────────────────────────────────────────
	case RibbonIcon::GizmoMove:   p.GizmoAxes(0); break;
	case RibbonIcon::GizmoScale:  p.GizmoAxes(1); break;
	case RibbonIcon::GizmoRotate:
		p.SetColor(DarkTheme::AxisZ); p.Ellipse(0, 0, 0.9f, 0.9f);
		p.SetColor(DarkTheme::AxisX); p.Ellipse(0, 0, 0.9f, 0.32f);
		p.SetColor(DarkTheme::AxisY); p.Ellipse(0, 0, 0.32f, 0.9f);
		break;
	case RibbonIcon::Move:
		p.Line(-0.9f, 0, 0.9f, 0);  p.Line(0, -0.9f, 0, 0.9f);
		p.Arrow( 0.9f, 0, 1, 0);    p.Arrow(-0.9f, 0, -1, 0);
		p.Arrow(0,  0.9f, 0, 1);    p.Arrow(0, -0.9f, 0, -1);
		break;
	case RibbonIcon::Rotate:
		p.Arc(0, 0, 0.75f, -60, 290);              // 시계 방향으로 230° 에서 끝난다
		p.Arrow(-0.48f, -0.57f, 0.77f, -0.64f);    // 끝점에서 진행 방향(접선)으로
		break;
	case RibbonIcon::Scale:
		p.Rect(-0.9f, 0.1f, 0.8f, 0.8f);
		p.Dashed(true);  p.Rect(-0.9f, -0.9f, 1.8f, 1.8f);  p.Dashed(false);
		p.Line(-0.1f, 0.1f, 0.65f, -0.65f);
		p.Arrow(0.65f, -0.65f, 1, -1);
		break;
	case RibbonIcon::Copy:
		p.Rect(-0.9f, -0.35f, 1.2f, 1.25f);
		p.Rect(-0.3f, -0.9f, 1.2f, 1.25f);
		break;
	case RibbonIcon::Array:
		for (int row = 0; row < 2; ++row)
			for (int col = 0; col < 3; ++col)
				p.Rect(-0.9f + col * 0.65f, -0.65f + row * 0.75f, 0.5f, 0.5f);
		break;
	case RibbonIcon::SelectAll:
		p.Dashed(true);  p.Rect(-0.9f, -0.9f, 1.8f, 1.8f);  p.Dashed(false);
		p.FillPoly({ { -0.45f, -0.45f }, { 0.45f, -0.45f }, { 0.45f, 0.45f }, { -0.45f, 0.45f } });
		break;
	case RibbonIcon::Delete:
		p.Line(-0.7f, -0.7f, 0.7f, 0.7f);  p.Line(0.7f, -0.7f, -0.7f, 0.7f);
		break;
	case RibbonIcon::ClearAll:   // 휴지통
		p.Line(-0.9f, -0.6f, 0.9f, -0.6f);
		p.Line(-0.3f, -0.9f, 0.3f, -0.9f);
		p.Poly({ { -0.65f, -0.6f }, { -0.5f, 0.9f }, { 0.5f, 0.9f }, { 0.65f, -0.6f } });
		p.Line(-0.2f, -0.25f, -0.15f, 0.55f);  p.Line(0.2f, -0.25f, 0.15f, 0.55f);
		break;
	case RibbonIcon::Undo:
		p.Arc(0, 0.25f, 0.7f, 180, 180);
		p.Arrow(-0.7f, 0.35f, 0, 1);
		break;
	case RibbonIcon::Redo:
		p.Arc(0, 0.25f, 0.7f, 180, 180);
		p.Arrow(0.7f, 0.35f, 0, 1);
		break;
	case RibbonIcon::ZoomExtents:   // 네 모서리 괄호 + 가운데 물체
		p.Poly({ { -0.9f, -0.4f }, { -0.9f, -0.9f }, { -0.4f, -0.9f } });
		p.Poly({ {  0.4f, -0.9f }, {  0.9f, -0.9f }, {  0.9f, -0.4f } });
		p.Poly({ {  0.9f,  0.4f }, {  0.9f,  0.9f }, {  0.4f,  0.9f } });
		p.Poly({ { -0.4f,  0.9f }, { -0.9f,  0.9f }, { -0.9f,  0.4f } });
		p.Rect(-0.35f, -0.35f, 0.7f, 0.7f);
		break;
	case RibbonIcon::Focus:         // 조준선
		p.Ellipse(0, 0, 0.55f, 0.55f);
		p.Line(0, -0.95f, 0, -0.55f);  p.Line(0, 0.55f, 0, 0.95f);
		p.Line(-0.95f, 0, -0.55f, 0);  p.Line(0.55f, 0, 0.95f, 0);
		p.Dot(0, 0);
		break;
	case RibbonIcon::Projection:    // 시야 절두체
		p.Poly({ { 0.9f, -0.8f }, { -0.9f, 0 }, { 0.9f, 0.8f } });
		p.Line(0.9f, -0.8f, 0.9f, 0.8f);
		p.Dashed(true);  p.Line(0.1f, -0.45f, 0.1f, 0.45f);  p.Dashed(false);
		break;
	case RibbonIcon::ViewIso:   p.IsoCube(0); break;
	case RibbonIcon::ViewTop:   p.IsoCube(1); break;
	case RibbonIcon::ViewFront: p.IsoCube(2); break;
	case RibbonIcon::ViewRight: p.IsoCube(3); break;

	// ── 2D ──────────────────────────────────────────────────────────────
	case RibbonIcon::Line:
		p.Line(-0.8f, 0.8f, 0.8f, -0.8f);
		p.Dot(-0.8f, 0.8f);  p.Dot(0.8f, -0.8f);
		break;
	case RibbonIcon::Rectangle: p.Rect(-0.85f, -0.6f, 1.7f, 1.2f); break;
	case RibbonIcon::Circle:    p.Ellipse(0, 0, 0.85f, 0.85f); p.Dot(0, 0); break;
	case RibbonIcon::Polygon:
		p.Poly({ { 0.9f, 0 }, { 0.45f, -0.78f }, { -0.45f, -0.78f },
		         { -0.9f, 0 }, { -0.45f, 0.78f }, { 0.45f, 0.78f } }, true);
		break;
	case RibbonIcon::Arc:
		p.Arc(0, 0.45f, 0.85f, 200, 140);
		p.Dot(-0.8f, 0.16f);  p.Dot(0.8f, 0.16f);  p.Dot(0, -0.4f);
		break;
	case RibbonIcon::Polyline:
		p.Poly({ { -0.9f, 0.7f }, { -0.3f, -0.5f }, { 0.3f, 0.4f }, { 0.9f, -0.7f } });
		p.Dot(-0.9f, 0.7f);  p.Dot(-0.3f, -0.5f);  p.Dot(0.3f, 0.4f);  p.Dot(0.9f, -0.7f);
		break;
	case RibbonIcon::Text:          // A
		p.Poly({ { -0.7f, 0.9f }, { 0, -0.9f }, { 0.7f, 0.9f } });
		p.Line(-0.4f, 0.25f, 0.4f, 0.25f);
		break;
	case RibbonIcon::Dimension:
		p.Line(-0.9f, -0.6f, -0.9f, 0.9f);  p.Line(0.9f, -0.6f, 0.9f, 0.9f);
		p.Line(-0.9f, 0.1f, 0.9f, 0.1f);
		p.Arrow(-0.9f, 0.1f, -1, 0);  p.Arrow(0.9f, 0.1f, 1, 0);
		p.Line(-0.3f, -0.4f, 0.3f, -0.4f);   // 치수 문자 자리
		break;
	case RibbonIcon::Offset:        // 같은 중심의 두 호
		p.Arc(0, 0.9f, 0.55f, 200, 140);
		p.Dashed(true);  p.Arc(0, 0.9f, 1.0f, 205, 130);  p.Dashed(false);
		break;
	case RibbonIcon::Mirror:
		p.Dashed(true);  p.Line(0, -0.95f, 0, 0.95f);  p.Dashed(false);
		p.Poly({ { -0.25f, -0.7f }, { -0.25f, 0.7f }, { -0.9f, 0.7f } }, true);
		p.FillPoly({ { 0.25f, -0.7f }, { 0.25f, 0.7f }, { 0.9f, 0.7f } });
		p.Poly({ { 0.25f, -0.7f }, { 0.25f, 0.7f }, { 0.9f, 0.7f } }, true);
		break;
	case RibbonIcon::Trim:          // 경계선에서 잘린 쪽은 점선
		p.Line(0.2f, -0.9f, 0.2f, 0.9f);
		p.Line(-0.9f, 0, 0.2f, 0);
		p.Dashed(true);  p.Line(0.2f, 0, 0.9f, 0);  p.Dashed(false);
		break;
	case RibbonIcon::Extend:        // 경계선까지 늘어난 부분은 점선 + 화살촉
		p.Line(0.8f, -0.9f, 0.8f, 0.9f);
		p.Line(-0.9f, 0, 0.0f, 0);
		p.Dashed(true);  p.Line(0.0f, 0, 0.75f, 0);  p.Dashed(false);
		p.Arrow(0.75f, 0, 1, 0);
		break;
	case RibbonIcon::Fillet:
		p.Line(-0.8f, 0.85f, -0.8f, -0.1f);
		p.Arc(-0.1f, -0.1f, 0.7f, 180, 90);
		p.Line(-0.1f, -0.8f, 0.85f, -0.8f);
		break;
	case RibbonIcon::Chamfer:
		p.Poly({ { -0.8f, 0.85f }, { -0.8f, -0.1f }, { -0.1f, -0.8f }, { 0.85f, -0.8f } });
		break;

	// ── 3D ──────────────────────────────────────────────────────────────
	case RibbonIcon::Box: p.IsoCube(0); break;
	case RibbonIcon::Sphere:
		p.Ellipse(0, 0, 0.85f, 0.85f);
		p.Ellipse(0, 0, 0.85f, 0.3f);
		p.Ellipse(0, 0, 0.3f, 0.85f);
		break;
	case RibbonIcon::Cylinder:
		p.Ellipse(0, -0.6f, 0.7f, 0.25f);
		p.Line(-0.7f, -0.6f, -0.7f, 0.6f);  p.Line(0.7f, -0.6f, 0.7f, 0.6f);
		p.EllipseArc(0, 0.6f, 0.7f, 0.25f, 0, 180);   // 바닥은 앞쪽 반만 보인다
		break;
	case RibbonIcon::Cone:
		p.Poly({ { -0.75f, 0.6f }, { 0, -0.9f }, { 0.75f, 0.6f } });
		p.Ellipse(0, 0.6f, 0.75f, 0.25f);
		break;
	case RibbonIcon::Torus:
		p.Ellipse(0, 0, 0.9f, 0.55f);
		p.Ellipse(0, -0.05f, 0.38f, 0.18f);
		break;
	case RibbonIcon::Extrude:       // 바닥 단면 + 위로 화살표
		p.FillPoly({ { -0.85f, 0.9f }, { 0.35f, 0.9f }, { 0.85f, 0.5f }, { -0.35f, 0.5f } });
		p.Poly({ { -0.85f, 0.9f }, { 0.35f, 0.9f }, { 0.85f, 0.5f }, { -0.35f, 0.5f } }, true);
		p.Line(0, 0.7f, 0, -0.9f);
		p.Arrow(0, -0.9f, 0, -1);
		break;
	case RibbonIcon::Revolve:       // 축 + 둘레를 도는 화살표
		p.Dashed(true);  p.Line(0, -0.95f, 0, 0.95f);  p.Dashed(false);
		p.Ellipse(0, 0, 0.85f, 0.32f);
		p.Arrow(0.15f, 0.32f, 1, 0);
		break;
	case RibbonIcon::Sweep: {       // 단면이 곡선 경로를 따라감
		auto pts = p.Map({ { -0.75f, 0.75f }, { -0.6f, -0.3f }, { 0.4f, 0.6f }, { 0.85f, -0.7f } });
		p.g.DrawBeziers(&p.pen, pts.data(), (INT)pts.size());
		p.FillEllipse(-0.75f, 0.75f, 0.22f, 0.22f);
		p.Ellipse(-0.75f, 0.75f, 0.22f, 0.22f);
		p.Arrow(0.85f, -0.7f, 0.4f, -1);
		break;
	}
	case RibbonIcon::Loft:          // 크기가 다른 두 단면을 잇는다
		p.Ellipse(0, 0.6f, 0.8f, 0.25f);
		p.Ellipse(0, -0.6f, 0.4f, 0.14f);
		p.Line(-0.8f, 0.6f, -0.4f, -0.6f);  p.Line(0.8f, 0.6f, 0.4f, -0.6f);
		break;
	case RibbonIcon::Shell:         // 속이 빈 그릇 단면
		p.FillPoly({ { -0.9f, -0.7f }, { -0.9f, 0.85f }, { 0.9f, 0.85f }, { 0.9f, -0.7f },
		             { 0.6f, -0.7f }, { 0.6f, 0.55f }, { -0.6f, 0.55f }, { -0.6f, -0.7f } });
		p.Poly({ { -0.9f, -0.7f }, { -0.9f, 0.85f }, { 0.9f, 0.85f }, { 0.9f, -0.7f },
		         { 0.6f, -0.7f }, { 0.6f, 0.55f }, { -0.6f, 0.55f }, { -0.6f, -0.7f } }, true);
		break;
	case RibbonIcon::Union:      p.BooleanCircles(0); break;
	case RibbonIcon::Difference: p.BooleanCircles(1); break;
	case RibbonIcon::Intersect:  p.BooleanCircles(2); break;
	case RibbonIcon::Slice:
		p.IsoCube(0);
		p.SetColor(DarkTheme::Accent);
		p.Dashed(true);  p.Line(-1.0f, 0.35f, 1.0f, -0.35f);  p.Dashed(false);
		break;

	// ── 도구 ────────────────────────────────────────────────────────────
	case RibbonIcon::Section:       // 정육면체 + 가로지르는 단면(강조색 면)
		p.FillPoly({ { -0.8f, 0.1f }, { 0, -0.35f }, { 0.8f, 0.1f }, { 0, 0.55f } });
		p.IsoCube(0);
		p.SetColor(DarkTheme::Accent);
		p.Poly({ { -0.8f, 0.1f }, { 0, -0.35f }, { 0.8f, 0.1f }, { 0, 0.55f } }, true);
		break;
	case RibbonIcon::Navigation:    // 출발점 → 굽은 경로(점선) → 목적지 깃발
		p.Dot(-0.8f, 0.75f);
		p.Dashed(true);
		p.Poly({ { -0.8f, 0.75f }, { -0.2f, 0.75f }, { -0.2f, -0.1f }, { 0.55f, -0.1f } });
		p.Dashed(false);
		p.Line(0.55f, -0.1f, 0.55f, -0.95f);
		p.FillPoly({ { 0.55f, -0.95f }, { 0.95f, -0.75f }, { 0.55f, -0.55f } });
		p.Poly({ { 0.55f, -0.95f }, { 0.95f, -0.75f }, { 0.55f, -0.55f } }, true);
		break;
	case RibbonIcon::Particles:     // 한 점에서 퍼지는 불티
		p.Dot(0, 0.75f);
		p.Line(0, 0.75f, -0.55f, -0.35f);  p.Dot(-0.65f, -0.55f);
		p.Line(0, 0.75f,  0.0f,  -0.5f);   p.Dot(0, -0.8f);
		p.Line(0, 0.75f,  0.55f, -0.35f);  p.Dot(0.65f, -0.55f);
		p.Dot(-0.85f, 0.1f);  p.Dot(0.85f, 0.1f);
		break;

	case RibbonIcon::None:
	default:
		break;
	}
}
