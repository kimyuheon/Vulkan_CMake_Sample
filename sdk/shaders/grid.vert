#version 450

// 무한 그리드 (ground plane Y=0)
// 6 vertices로 전체 화면을 덮는 두 개의 삼각형을 그림
// fragment shader에서 각 픽셀의 카메라 ray를 ground plane과 교차시켜 world 좌표 계산

layout(location = 0) out vec3 nearPoint;
layout(location = 1) out vec3 farPoint;

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
} ubo;

// NDC(clip) 공간 화면 덮는 사각형
const vec3 gridPlane[6] = vec3[](
    vec3( 1.0,  1.0, 0.0), vec3(-1.0, -1.0, 0.0), vec3(-1.0,  1.0, 0.0),
    vec3(-1.0, -1.0, 0.0), vec3( 1.0,  1.0, 0.0), vec3( 1.0, -1.0, 0.0)
);

vec3 unproject(float x, float y, float z) {
    mat4 viewInv = inverse(ubo.view);
    mat4 projInv = inverse(ubo.projection);
    vec4 p = viewInv * projInv * vec4(x, y, z, 1.0);
    return p.xyz / p.w;
}

void main() {
    vec3 p = gridPlane[gl_VertexIndex];
    // Vulkan: near z=0, far z=1
    nearPoint = unproject(p.x, p.y, 0.0);
    farPoint  = unproject(p.x, p.y, 1.0);
    gl_Position = vec4(p, 1.0);
}
