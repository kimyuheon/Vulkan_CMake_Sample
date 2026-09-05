#version 450

// 단면 캡 — 절단 평면 위에 놓이는 큰 사각형. 정점 버퍼 없이 gl_VertexIndex 로 6정점 생성.
// 이 사각형을 **월드 공간 평면 위**에 그리므로 깊이 테스트가 정상 동작한다
// (전체화면 사각형으로 그리면 앞에 있는 다른 객체를 덮어써 버린다).

layout(push_constant) uniform Push {
    vec4 planePoint;   // xyz = 평면 위 한 점, w = 사각형 반폭(월드)
    vec4 axisU;        // xyz = 평면 내 축 U
    vec4 axisV;        // xyz = 평면 내 축 V
    vec4 capColor;     // rgb = 캡 색
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
    // (-1,-1) (1,-1) (1,1) / (-1,-1) (1,1) (-1,1)
    const vec2 corners[6] = vec2[6](
        vec2(-1.0, -1.0), vec2( 1.0, -1.0), vec2( 1.0,  1.0),
        vec2(-1.0, -1.0), vec2( 1.0,  1.0), vec2(-1.0,  1.0));
    vec2 c = corners[gl_VertexIndex];
    float halfSize = push.planePoint.w;   // 'half' 은 GLSL 예약어라 다른 이름 사용
    vec3 world = push.planePoint.xyz + push.axisU.xyz * (c.x * halfSize)
                                     + push.axisV.xyz * (c.y * halfSize);
    gl_Position = ubo.projection * ubo.view * vec4(world, 1.0);
}
