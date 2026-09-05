#version 450

layout(location = 0) in  vec2 fragUV;
layout(location = 0) out vec4 outColor;

// binding 0: JFA distance field computed from the full (occluded) silhouette
layout(set = 0, binding = 0) uniform sampler2D jfaResult;
// binding 1: visible mask (depth-tested) — white where the selected object is NOT occluded
layout(set = 0, binding = 1) uniform sampler2D visibleMask;

layout(push_constant) uniform Push {
    vec2  resolution;        // JFA / framebuffer size in physical pixels (full swapchain extent)
    float outlineThickness;  // outline width in pixels (e.g. 3.0)
} push;

void main() {
    // fragUV 는 viewport 의 0..1 좌표 — viewport 가 central rect 이면 central 안 0..1.
    // 하지만 JFA 텍스처는 full framebuffer extent. gl_FragCoord 로 절대 픽셀 좌표를 받아서
    // full resolution 으로 나눠야 JFA 의 같은 텍셀을 정확히 샘플함.
    vec2 jfaUV = gl_FragCoord.xy / push.resolution;
    vec2 nearestSeedUV = texture(jfaResult, jfaUV).rg;

    // Background: no seed nearby → skip
    if (nearestSeedUV.x < 0.0) discard;

    // pixel-space 거리 계산 — gl_FragCoord 와 seed*resolution 모두 framebuffer 픽셀.
    vec2  pixelCoord     = gl_FragCoord.xy;
    vec2  seedPixelCoord = nearestSeedUV * push.resolution;
    float dist = distance(pixelCoord, seedPixelCoord);

    // Draw outline ring just outside the full silhouette
    if (dist < 0.5 || dist > push.outlineThickness + 0.5) discard;

    // Determine visibility at the nearest seed position
    float visible = texture(visibleMask, nearestSeedUV).r;

    // Solid yellow where visible, semi-transparent ghost where occluded
    float alpha = (visible > 0.5) ? 1.0 : 0.15;
    outColor = vec4(1.0, 1.0, 0.0, alpha);
}
