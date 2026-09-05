#version 450

// 인스턴스드 렌더 (simple_shader.vert 의 plain 경로 전용 변형).
// 동일 메시를 공유하는 무텍스처·비선택 오브젝트들을 draw call 1개로 배칭.
// 아웃라인/실루엣 분기 없음 — 선택/특수 오브젝트는 기존 per-object 경로가 그림.

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 color;
layout (location = 2) in vec3 normal;
layout (location = 3) in vec2 uv;

// per-instance (binding=1, VK_VERTEX_INPUT_RATE_INSTANCE)
layout (location = 4)  in vec4 instModel0;   // modelMatrix 열 0~3
layout (location = 5)  in vec4 instModel1;
layout (location = 6)  in vec4 instModel2;
layout (location = 7)  in vec4 instModel3;
layout (location = 8)  in vec4 instNormal0;  // normalMatrix 열 0~3
layout (location = 9)  in vec4 instNormal1;
layout (location = 10) in vec4 instNormal2;
layout (location = 11) in vec4 instNormal3;
layout (location = 12) in vec4 instColor;    // rgb = 오브젝트 색 (0이면 프래그가 버텍스색 폴백)

layout (location = 0) out vec3 fragColor;
layout (location = 1) out vec3 fragPosWorld;
layout (location = 2) out vec3 fragNormalWorld;
layout (location = 3) out vec2 fragTexCoord;
layout (location = 4) out vec3 fragObjColor;

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

void main() {
    mat4 modelMatrix  = mat4(instModel0, instModel1, instModel2, instModel3);
    mat4 normalMatrix = mat4(instNormal0, instNormal1, instNormal2, instNormal3);

    vec4 positionWorld = modelMatrix * vec4(position, 1.0);
    gl_Position = ubo.projection * ubo.view * positionWorld;

    fragNormalWorld = normalize(mat3(normalMatrix) * normal);
    fragPosWorld = positionWorld.xyz;
    fragColor = color;
    fragTexCoord = uv;
    fragObjColor = instColor.rgb;
}
