#version 450

// 단면 캡 — 스텐실이 "솔리드 내부" 로 표시한 픽셀만 단색으로 채운다(파이프라인 스텐실 테스트).
// 조명 계산 없이 평평한 색 — 절단면은 재질이 아니라 "잘린 자리" 라 단색이 CAD 관례.

layout (location = 0) out vec4 outColor;

layout(push_constant) uniform Push {
    vec4 planePoint;
    vec4 axisU;
    vec4 axisV;
    vec4 capColor;
} push;

void main() {
    outColor = vec4(push.capColor.rgb, 1.0);
}
