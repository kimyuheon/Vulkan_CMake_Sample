#version 450

// 인스턴스드 렌더 프래그먼트 — simple_shader.frag 의 plain(비선택·무텍스처) 경로와
// 동일한 조명. 오브젝트 색은 push 대신 per-instance varying(fragObjColor)으로 받음.

layout (location = 0) in vec3 fragColor;
layout (location = 1) in vec3 fragPosWorld;
layout (location = 2) in vec3 fragNormalWorld;
layout (location = 3) in vec2 fragTexCoord;
layout (location = 4) in vec3 fragObjColor;

layout (location = 0) out vec4 outColor;

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
    int clipCount;
    int _pad0;
    vec4 clipPlanes[6];
} ubo;

void main(){
    // 단면(Section) 클립 — 어떤 활성 평면이든 양의 반공간이면 버림 (실시간 단면).
    for (int ci = 0; ci < ubo.clipCount; ci++) {
        if (dot(ubo.clipPlanes[ci].xyz, fragPosWorld) + ubo.clipPlanes[ci].w > 0.0) discard;
    }

    // 오브젝트 색이 사실상 0이면 버텍스 색 사용 (simple_shader.frag 와 동일 규칙)
    vec3 baseColor = (length(fragObjColor) > 0.01) ? fragObjColor : fragColor;

    vec3 finalColor;

    if (ubo.lightingEnable == 1) {
        vec3 diffuseLight = vec3(0.0);
        vec3 specularLight = vec3(0.0);
        vec3 ambientLight = ubo.ambientLightColor.xyz * ubo.ambientLightColor.w;
        vec3 normalWorld = normalize(fragNormalWorld);

        vec3 cameraRightWorld = ubo.inverseView[3].xyz;
        vec3 viewDirection = normalize(cameraRightWorld - fragNormalWorld);

        // Key directional light
        {
            vec3 lightDir = normalize(ubo.dirLightDirection.xyz);
            float diff = max(dot(normalWorld, -lightDir), 0.0);
            diffuseLight += ubo.dirLightColor.xyz * ubo.dirLightColor.w * diff;
        }
        // Fill light (역광)
        {
            vec3 lightDir = normalize(ubo.fillLightDirection.xyz);
            float diff = max(dot(normalWorld, -lightDir), 0.0);
            diffuseLight += ubo.fillLightColor.xyz * ubo.fillLightColor.w * diff;
        }

        for (int i = 0; i < ubo.numLights; i++) {
            PointLight light = ubo.pointLights[i];

            vec3 directionToLight = light.position.xyz - fragPosWorld;
            float attenuation = 1.0 / dot(directionToLight, directionToLight);
            vec3 lightColor = light.color.xyz * light.color.w * attenuation;

            vec3 directionToLightNorm = normalize(directionToLight);
            float cosAngIncidence = max(dot(normalWorld, directionToLightNorm), 0);

            diffuseLight += lightColor * cosAngIncidence;

            vec3 halfAngle = normalize(directionToLightNorm + viewDirection);
            float blinnTerm = dot(normalWorld, halfAngle);
            blinnTerm = clamp(blinnTerm, 0.0, 1.0);
            blinnTerm = pow(blinnTerm, 32.0);
            specularLight += lightColor * blinnTerm;
        }

        finalColor = (diffuseLight + ambientLight) * baseColor + specularLight;
    } else {
        finalColor = baseColor * 0.8;
    }

    outColor = vec4(finalColor, 1.0);
}
