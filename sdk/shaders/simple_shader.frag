#version 450

layout (location = 0) in vec3 fragColor;
layout (location = 1) in vec3 fragPosWorld;
layout (location = 2) in vec3 fragNormalWorld;
layout (location = 3) in vec2 fragTexCoord;

layout (location = 0) out vec4 outColor;

// ⚠️ 정확히 128 바이트 — simple_shader.vert / C++ SimplePushConstantData 와 1:1 일치.
//    normalRows[i].xyz = 법선행렬 행, normalRows[i].w = color.r/g/b
//    misc = (isSelected, hasTexture, textureScale, 예약)
layout(push_constant) uniform Push {
    mat4 modelMatrix;
    vec4 normalRows[3];
    vec4 misc;
} push;

// ⚠️ 128B 를 못 넘기므로 슬롯을 압축해 쓴다 (C++ 쪽 세 push 구조체와 1:1 일치).
//   normalRows[0].w = 색 (채널당 8비트: r + g*256 + b*65536)
//   normalRows[1].w = UV 오프셋 U,  [2].w = UV 오프셋 V
//   misc = (isSelected, 플래그+각도, U배율, V배율)
//   플래그 비트: 1=텍스처 있음, 2=단면 클립 적용, 4=반복 끄기
vec3 pushColor() {
    float p = push.normalRows[0].w;
    float b = floor(p / 65536.0);
    float g = floor((p - b * 65536.0) / 256.0);
    float r = p - b * 65536.0 - g * 256.0;
    return vec3(r, g, b) / 255.0;
}
int   pushIsSelected()   { return int(push.misc.x); }
int   pushFlags()        { return int(push.misc.y); }
int   pushHasTexture()   { return (pushFlags() & 1); }
bool  pushNoRepeat()     { return (pushFlags() & 4) != 0; }
float pushTextureAngle() { return fract(push.misc.y) * 6.28318531; }   // 라디안
float pushTextureScale() { return push.misc.z; }                      // 음수 = unlit
vec2  pushTextureScale2(){ return vec2(abs(push.misc.z), abs(push.misc.w)); }
vec2  pushTexOffset()    { return vec2(push.normalRows[1].w, push.normalRows[2].w); }
// 0 이면 이 객체엔 단면 클립을 적용하지 않는다 ("선택한 객체만" 옵션).
bool  pushClipEnabled()  { return (pushFlags() & 2) != 0; }

struct PointLight {
    vec4 position;
    vec4 color;
};

layout(set = 0, binding = 0) uniform GlobalUbo {
    mat4 projection;
    mat4 view;
    mat4 inverseView;
    vec4 ambientLightColor;
    vec4 dirLightDirection;   // xyz=normalized dir (light travel), w unused
    vec4 dirLightColor;
    vec4 fillLightDirection;
    vec4 fillLightColor;       // xyz=color, w=intensity
    PointLight pointLights[10];
    int numLights;
    int lightingEnable;
    int clipCount;
    int _pad0;
    vec4 clipPlanes[6];
} ubo;

layout (set = 1, binding = 0) uniform sampler2D texSampler;

void main(){
    // 단면(Section) 클립 — 어떤 활성 평면이든 양의 반공간이면 이 픽셀 버림 (실시간 단면).
    if (pushClipEnabled()) {
        for (int ci = 0; ci < ubo.clipCount; ci++) {
            if (dot(ubo.clipPlanes[ci].xyz, fragPosWorld) + ubo.clipPlanes[ci].w > 0.0) discard;
        }
    }

    // 아웃라인 패스: 단색 출력 후 종료 (2=3D 선택, 3=평면 선택)
    if (pushIsSelected() == 2 || pushIsSelected() == 3) {
        outColor = vec4(1.0, 0.8, 0.0, 1.0);  // 주황빛 노란색
        return;
    }

    // 치수/문자 주석 (6) — 정점 색(파트별: 치수선/보조선/화살표/문자 개별) 을 unlit 로.
    if (pushIsSelected() == 6) {
        outColor = vec4(fragColor, 1.0);
        return;
    }
    // 스케치 프리뷰 / 엣지 unlit (4) · 실루엣 unlit (5) — pushColor() 로 unlit.
    if (pushIsSelected() == 4 || pushIsSelected() == 5) {
        outColor = vec4(pushColor(), 1.0);
        return;
    }

    vec3 baseColor;
    bool textureDropped = false;   // 반복 끄기 + 범위 밖 → 텍스처 대신 색
    if (pushHasTexture() == 1) {
        // UVW 매핑 — 배율(U/V 개별) → 회전 → 오프셋 순.
        // 3ds Max 의 UVW Map 과 같은 순서라 결과를 예상할 수 있다.
        vec2 uv = fragTexCoord * pushTextureScale2();
        float a = pushTextureAngle();
        if (a != 0.0) {
            float s = sin(a), c = cos(a);
            uv = vec2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
        }
        uv += pushTexOffset();
        // 반복 끄기 — 로고·간판처럼 한 번만 붙일 때. 0~1 밖은 오브젝트 색으로 떨어진다.
        if (pushNoRepeat() && (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)) {
            baseColor = pushColor();
            textureDropped = true;
        } else {
            baseColor = texture(texSampler, uv).rgb;
        }
    } else if (length(pushColor()) > 0.01) {
        baseColor = pushColor();  // GameObject의 color 사용
    } else {
        baseColor = fragColor;   // 버텍스 색상 사용
    }

    // 음수 textureScale = 이미지 언더레이. 캡처/OCR 원본 색을 조명과 무관하게 표시한다.
    if (pushHasTexture() == 1 && pushTextureScale() < 0.0) {
        outColor = vec4(baseColor, 1.0);
        return;
    }

    vec3 finalColor;

    if (ubo.lightingEnable == 1) {
        // 조명이 켜져 있을 때: 방향성 조명 계산
        // 각 프레임마다 빛까지의 방향 계산
        vec3 diffuseLight = vec3(0.0);
        vec3 specularLight = vec3(0.0);
        vec3 ambientLight = ubo.ambientLightColor.xyz * ubo.ambientLightColor.w;
        vec3 normalWorld = normalize(fragNormalWorld);

        vec3 cameraRightWorld = ubo.inverseView[3].xyz;
        vec3 viewDirection = normalize(cameraRightWorld - fragNormalWorld);

        // Key directional light (CAD 기본 조명 = 태양광) — 감쇠 없는 평행광
        {
            vec3 lightDir = normalize(ubo.dirLightDirection.xyz);
            float diff = max(dot(normalWorld, -lightDir), 0.0);
            diffuseLight += ubo.dirLightColor.xyz * ubo.dirLightColor.w * diff;
        }
        // Fill light (역광) — Key 반대 방향에서 약하게. 그림자에 빛을 살짝 채워
        // dent / 오목면 (사과 가운데, 도자기 안쪽) 가독성 ↑. 강도는 Key 의 절반 이하.
        {
            vec3 lightDir = normalize(ubo.fillLightDirection.xyz);
            float diff = max(dot(normalWorld, -lightDir), 0.0);
            diffuseLight += ubo.fillLightColor.xyz * ubo.fillLightColor.w * diff;
        }

        for(int i = 0; i < ubo.numLights; i++) {
            PointLight light = ubo.pointLights[i];

            vec3 directionToLight = light.position.xyz - fragPosWorld;
            // 거리 감쇠 계산
            float attenuation = 1.0 / dot(directionToLight, directionToLight);
            // 감쇠가 적용된 빛 색상
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
        // 조명이 꺼져 있을 때: 균일한 밝기 (플랫 셰이딩)
        finalColor = baseColor * 0.8;
    }

    outColor = vec4(finalColor, 1.0);
}
