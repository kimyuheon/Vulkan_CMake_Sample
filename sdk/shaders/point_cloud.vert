#version 450

// 점군 렌더 — POINT_LIST 토폴로지.
//
// gl_PointSize 를 쓰는 이유: 빌보드 quad 로 확장하면 점 하나가 4 vertex + 6 index 가 되어
// 메모리가 4배 이상으로 뛴다. 점군은 개수가 곧 비용이라 그 배수가 치명적이다.
// Vulkan 스펙이 pointSizeRange 최소 [1.0, 64.0] 을 보장하므로 이 방식은 V3DV(라즈베리파이)·
// MoltenVK 를 포함한 어떤 구현에서도 동작한다.
//
// vertex layout — LotPointCloud::PointVertex (16 바이트):
//   location 0 : position (float3)
//   location 1 : color    (R8G8B8A8_UNORM → 셰이더에서 0..1 vec4)

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 inColor;

layout(location = 0) out vec4 v_color;
// 단면 클립 판정용 — 프래그먼트에서 평면과 비교해야 하므로 월드 좌표를 넘긴다.
// 점 하나가 차지하는 프래그먼트들은 사실상 같은 월드 위치라 정점값을 그대로 써도 된다.
layout(location = 1) out vec3 v_worldPos;

struct PointLight {
    vec4 position;
    vec4 color;
};

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
    // ⚠️ 메시 셰이더의 GlobalUbo 와 **같은 꼬리**를 달아야 한다. 같은 버퍼를 읽으므로
    //    여기서 빼먹으면 오프셋은 맞아도 clipPlanes 에 접근할 수 없다.
    int clipCount;
    int _pad0;
    vec4 clipPlanes[6];
} ubo;

layout(push_constant) uniform Push {
    mat4 modelMatrix;
    vec3 tint;          // 점 자체 색과 곱함. 객체 색을 그대로 쓰고 싶으면 흰색.
    float pointSizePx;  // 화면 픽셀 크기
    int useVertexColor; // 0 = tint 만 사용(색 없는 소스), 1 = 정점색 × tint
    int highlight;      // 0=일반, 1=선택(노랑), 2=호버(하늘색)
    int clipEnable;     // 0 이면 이 점군엔 단면 클립을 적용하지 않는다
} push;

void main() {
    vec4 worldPos = push.modelMatrix * vec4(position, 1.0);
    v_worldPos = worldPos.xyz;
    gl_Position = ubo.projection * ubo.view * worldPos;

    // 거리에 따라 점이 작아지지 않게 화면 픽셀 크기로 고정한다.
    // 원근 축소를 넣으면 먼 점이 1픽셀 미만이 되어 점군에 구멍이 뚫린 것처럼 보인다.
    // 스펙 보장 상한(64)을 넘기면 구현에 따라 클램프되거나 그리지 않을 수 있어 잘라준다.
    gl_PointSize = clamp(push.pointSizePx, 1.0, 64.0);

    vec3 base = (push.useVertexColor != 0) ? inColor.rgb * push.tint : push.tint;
    v_color = (push.highlight == 1) ? vec4(1.0, 0.85, 0.0, 1.0)
            : (push.highlight == 2) ? vec4(0.45, 0.80, 1.0, 1.0)
                                    : vec4(base, inColor.a);
}
