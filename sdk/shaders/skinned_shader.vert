#version 450

// 스킨드 메시(캐릭터) 전용 버텍스 셰이더.
// binding 0 = 일반 정점(정적 메시와 동일 레이아웃), binding 1 = 스킨 속성.
// 정적 메시는 binding 1 자체가 없으므로 이 셰이더/파이프라인을 타지 않는다.
layout (location = 0) in vec3 position;
layout (location = 1) in vec3 color;
layout (location = 2) in vec3 normal;
layout (location = 3) in vec2 uv;
layout (location = 4) in uvec4 inJoints;    // 스킨 joints[] 인덱스 4개
layout (location = 5) in vec4  inWeights;   // 합 = 1 (로더가 정규화)

layout (location = 0) out vec3 fragColor;
layout (location = 1) out vec3 fragPosWorld;
layout (location = 2) out vec3 fragNormalWorld;
layout (location = 3) out vec2 fragTexCoord;

// simple_shader 와 동일한 128바이트 압축 레이아웃 (프래그먼트 셰이더를 공유하므로 필수).
layout(push_constant) uniform Push {
    mat4 modelMatrix;
    vec4 normalRows[3];
    vec4 misc;
} push;

mat3 pushNormalMatrix() {
    return mat3(push.normalRows[0].xyz, push.normalRows[1].xyz, push.normalRows[2].xyz);
}

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

// set 2 = 본 행렬. set 1 은 텍스처(프래그먼트) 라 비워둘 수 없어 그대로 둔다.
// mat4 × 128 = 8KB — Vulkan 보장 최소 maxUniformBufferRange(16KB) 안.
const int MAX_JOINTS = 128;
layout(set = 2, binding = 0) uniform BoneUbo {
    mat4 joints[MAX_JOINTS];
} bones;

void main() {
    // Linear Blend Skinning — 정점을 여러 본이 가중치만큼 나눠 끈다.
    mat4 skinMat =
        inWeights.x * bones.joints[inJoints.x] +
        inWeights.y * bones.joints[inJoints.y] +
        inWeights.z * bones.joints[inJoints.z] +
        inWeights.w * bones.joints[inJoints.w];

    vec4 positionWorld = push.modelMatrix * skinMat * vec4(position, 1.0);
    gl_Position = ubo.projection * ubo.view * positionWorld;

    // 법선도 같은 변형을 받아야 조명이 맞는다. 균등 스케일 가정(비균등이면
    // inverse-transpose 가 필요하지만 캐릭터 본은 보통 회전+이동뿐이라 이걸로 충분).
    fragNormalWorld = normalize(pushNormalMatrix() * mat3(skinMat) * normal);
    fragPosWorld    = positionWorld.xyz;
    fragColor       = color;
    fragTexCoord    = uv;
}
