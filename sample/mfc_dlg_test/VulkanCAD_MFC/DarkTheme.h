#pragma once
// 다크 테마 색 — 리본·명령행·대화상자가 함께 쓴다. 엔진(ImGui) UI 와 같은 톤.
// 색을 바꾸려면 여기만 고친다.

namespace DarkTheme
{
	constexpr COLORREF TabBarBg     = RGB( 30,  30,  30);   // 탭 줄
	constexpr COLORREF BodyBg       = RGB( 43,  43,  45);   // 그룹이 놓이는 본문
	constexpr COLORREF CaptionBg    = RGB( 52,  52,  55);   // 그룹 이름 띠
	constexpr COLORREF Separator    = RGB( 70,  70,  74);   // 그룹 사이 세로선·맨 아래 선
	constexpr COLORREF Hover        = RGB( 62,  62,  66);   // 버튼 위에 마우스
	constexpr COLORREF Pressed      = RGB( 80,  80,  86);   // 버튼 누르는 중
	constexpr COLORREF Text         = RGB(230, 230, 230);
	constexpr COLORREF TextDim      = RGB(150, 150, 155);   // 비활성 탭·그룹 이름
	constexpr COLORREF Accent       = RGB(107, 171, 245);   // 활성 탭 밑줄·켜진 버튼 (엔진 강조 파랑)

	// 대화상자 바탕 — 리본·3D 뷰와 이어지게 어둡게.
	constexpr COLORREF DialogBg     = RGB( 37,  37,  38);

	// 명령행
	constexpr COLORREF InputBg      = RGB( 30,  30,  30);   // 입력란·기록 창 바탕
	constexpr COLORREF Prompt       = RGB(255, 209,  77);   // 진행 중 도구의 안내문 (엔진 명령행과 같은 앰버)

	// 기즈모 아이콘의 축 색 (X 빨강 · Y 초록 · Z 파랑)
	constexpr COLORREF AxisX        = RGB(230,  85,  85);
	constexpr COLORREF AxisY        = RGB(115, 200,  95);
	constexpr COLORREF AxisZ        = RGB( 95, 145, 240);
}
