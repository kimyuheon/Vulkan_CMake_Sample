#version 450

// Thick line fragment shader — 단색 출력.
// 향후 dashed/dotted 패턴, anti-aliasing, cap/join 추가 시 이 셰이더 확장.
// macOS sRGB 정규화 이슈는 v_color 가 이미 push 에서 직접 받은 값이라 영향 없음.

layout(location = 0) in vec3 v_color;
layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(v_color, 1.0);
}
