#version 450

// 단면 캡 — 스텐실 카운트 패스. 색 기록은 파이프라인에서 막혀 있고(colorWriteMask=0),
// 여기서는 **잘려나간 쪽 프래그먼트를 버리는 것**만 담당한다(본 렌더와 동일한 클립 규칙).
// 그래야 "평면 앞쪽 면이 사라져 뒷면만 남은" 픽셀이 스텐실에 잡혀 잘린 단면이 된다.

layout (location = 0) in vec3 fragPosWorld;

layout (location = 0) out vec4 outColor;   // colorWriteMask=0 이라 실제로 안 써짐

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
    for (int ci = 0; ci < ubo.clipCount; ci++) {
        if (dot(ubo.clipPlanes[ci].xyz, fragPosWorld) + ubo.clipPlanes[ci].w > 0.0) discard;
    }
    outColor = vec4(0.0);
}
