#version 450

// 단면 캡 — 스텐실 카운트 패스용 최소 정점 셰이더.
// 색을 안 쓰므로 텍스처 디스크립터(set=1)도 필요 없고, 푸시 상수는 modelMatrix 하나(64B)뿐.
// (SimpleRenderSystem 셰이더를 재사용하면 128B 푸시 + 텍스처 셋 바인딩이 딸려와 낭비.)

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 color;     // 미사용 (정점 레이아웃 호환용)
layout (location = 2) in vec3 normal;    // 미사용
layout (location = 3) in vec2 uv;        // 미사용

layout (location = 0) out vec3 fragPosWorld;   // 프래그에서 클립 평면 판정에 사용

layout(push_constant) uniform Push {
    mat4 modelMatrix;
} push;

struct PointLight { vec4 position; vec4 color; };

layout(set = 0, binding = 0) uniform GlobalUbo {
    mat4 projection;
    mat4 view;
    mat4 inverseView;
    vec4 ambientLightColor;
    vec4 dirLightDirection;
    vec4 dirLightColor;
    vec4 fillLightDirection;
    vec4 fillLightColor;
    PointLight pointLights[10];
    int numLights;
    int lightingEnable;
    int clipCount;
    int _pad0;
    vec4 clipPlanes[6];
} ubo;

void main() {
    vec4 worldPos = push.modelMatrix * vec4(position, 1.0);
    fragPosWorld = worldPos.xyz;
    gl_Position = ubo.projection * ubo.view * worldPos;
}
