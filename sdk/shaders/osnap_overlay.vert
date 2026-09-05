#version 450

// 2D screen-space 오버레이 (OSnap 심볼 / OTRACK 라인 등).
// 정점 좌표가 이미 픽셀 단위 — 셰이더는 viewportSize 로 NDC 변환만 수행.
//
//   입력 좌표계: top-left=(0,0), bottom-right=(w,h)  ← ImGui DrawList 와 동일
//   Vulkan NDC: top-left=(-1,-1), bottom-right=(+1,+1)
//
// 한쪽 축만 뒤집히는 OpenGL/Metal 차이는 다른 파이프라인이 처리하므로
// 여기서는 단순 선형 매핑만 한다.

layout(location = 0) in vec2 inPosPx;
layout(location = 1) in vec4 inColor;

layout(location = 0) out vec4 v_color;

layout(push_constant) uniform Push {
    vec2 viewportSize;  // pixels
} push;

void main() {
    vec2 ndc = inPosPx / push.viewportSize * 2.0 - 1.0;
    gl_Position = vec4(ndc, 0.0, 1.0);
    v_color = inColor;
}
