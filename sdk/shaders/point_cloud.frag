#version 450

// 점군 프래그먼트 — 단면 클립 + 사각 점을 둥글게 깎기.
//
// POINT_LIST 로 그리면 기본이 정사각형이라, 점 크기를 키우면 격자 무늬처럼 보인다.
// gl_PointCoord (점 내부 0..1 좌표) 로 중심에서 반지름 밖을 버려 원으로 만든다.
// alpha blending 없이 discard 로 처리 — 점군은 수가 많아 정렬 없는 블렌딩이 지저분해진다.

layout(location = 0) in vec4 v_color;
layout(location = 1) in vec3 v_worldPos;
layout(location = 0) out vec4 outColor;

struct PointLight {
    vec4 position;
    vec4 color;
};

// 메시 셰이더와 **같은 GlobalUbo** — 단면 평면을 읽으려면 꼬리까지 그대로 선언해야 한다.
// (점군은 조명을 안 쓰지만 버퍼 레이아웃이 같아야 오프셋이 맞는다)
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

layout(push_constant) uniform Push {
    mat4 modelMatrix;
    vec3 tint;
    float pointSizePx;
    int useVertexColor;
    int highlight;
    int clipEnable;
} push;

void main() {
    // 단면(Section) 클립 — 메시와 같은 규칙: 활성 평면 중 하나라도 양의 반공간이면 버린다.
    // 이게 있어야 스캔을 특정 높이로 잘라 평면도 윤곽을 볼 수 있다.
    if (push.clipEnable != 0) {
        for (int ci = 0; ci < ubo.clipCount; ci++) {
            if (dot(ubo.clipPlanes[ci].xyz, v_worldPos) + ubo.clipPlanes[ci].w > 0.0) discard;
        }
    }

    // 점 크기가 1픽셀이면 gl_PointCoord 가 (0.5,0.5) 한 점뿐이라 discard 되지 않는다.
    vec2 d = gl_PointCoord - vec2(0.5);
    if (dot(d, d) > 0.25) discard;   // 반지름 0.5 밖 → 버림

    outColor = v_color;
}
