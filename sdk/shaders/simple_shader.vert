#version 450

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 color;
layout (location = 2) in vec3 normal;
layout (location = 3) in vec2 uv;

layout (location = 0) out vec3 fragColor;
layout (location = 1) out vec3 fragPosWorld;
layout (location = 2) out vec3 fragNormalWorld;
layout (location = 3) out vec2 fragTexCoord;

// ⚠️ 정확히 128 바이트 — Vulkan 보장 최소치(maxPushConstantsSize=128)에 맞춘 압축 레이아웃.
//    Mali/Adreno/AMD 다수가 128 만 주므로 초과하면 그 기기에서 파이프라인 생성이 실패한다.
//    normalRows[i].xyz = 법선행렬 행,  normalRows[i].w = color.r/g/b
//    misc = (isSelected, hasTexture, textureScale, 예약)
//    ⚠️ C++ 쪽 SimplePushConstantData / LinePushConstantData 와 1:1 일치해야 함.
layout(push_constant) uniform Push {
    mat4 modelMatrix;
    vec4 normalRows[3];
    vec4 misc;
} push;

mat3 pushNormalMatrix() {
    return mat3(push.normalRows[0].xyz, push.normalRows[1].xyz, push.normalRows[2].xyz);
}
// 색은 채널당 8비트로 한 칸에 압축돼 온다 — 남은 [1].w/[2].w 는 UV 오프셋이 쓴다.
// (frag 의 pushColor() 와 반드시 같은 방식)
vec3 pushColor() {
    float p = push.normalRows[0].w;
    float b = floor(p / 65536.0);
    float g = floor((p - b * 65536.0) / 256.0);
    float r = p - b * 65536.0 - g * 256.0;
    return vec3(r, g, b) / 255.0;
}
int   pushIsSelected()   { return int(push.misc.x); }
int   pushHasTexture()   { return (int(push.misc.y) & 1); }
float pushTextureScale() { return push.misc.z; }

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
    vec4 positionWorld = push.modelMatrix * vec4(position, 1.0);

    if (pushIsSelected() == 2 || pushIsSelected() == 5) {
        // 백페이스 아웃라인 (파이프라인: VK_CULL_MODE_FRONT_BIT)
        //   2 = 선택 하이라이트 (노란색 고정)
        //   5 = 일반 실루엣 (pushColor()로 unlit)
        // 스크린 공간 노멀 방향으로 확장, 뷰와 평행한 경우 중심 기준 방사형 폴백
        gl_Position = ubo.projection * ubo.view * positionWorld;
        vec3 worldNormal = normalize(pushNormalMatrix() * normal);
        vec3 viewNormal = mat3(ubo.view) * worldNormal;
        vec2 screenDir = vec2(
            ubo.projection[0][0] * viewNormal.x,
            ubo.projection[1][1] * viewNormal.y
        );
        float len = length(screenDir);
        // 실루엣은 좀 더 얇게(0.004), 선택은 기존 0.007
        float expandAmt = (pushIsSelected() == 5) ? 0.004 : 0.007;
        if (len > 0.001) {
            screenDir /= len;
            gl_Position.xy += screenDir * expandAmt * gl_Position.w;
        } else {
            // 노멀이 뷰 방향과 평행 → 오브젝트 중심 기준 방사형 확장
            vec4 clipCenter = ubo.projection * ubo.view * vec4(push.modelMatrix[3].xyz, 1.0);
            screenDir = gl_Position.xy / gl_Position.w - clipCenter.xy / clipCenter.w;
            len = length(screenDir);
            if (len > 0.001) {
                screenDir /= len;
                gl_Position.xy += screenDir * expandAmt * gl_Position.w;
            }
        }
    } else if (pushIsSelected() == 3) {
        // 평면 오브젝트: 오브젝트 중심에서 방사형 확장 (floor 등)
        gl_Position = ubo.projection * ubo.view * positionWorld;
        vec4 clipCenter = ubo.projection * ubo.view * vec4(push.modelMatrix[3].xyz, 1.0);
        vec2 screenDir = gl_Position.xy / gl_Position.w - clipCenter.xy / clipCenter.w;
        float len = length(screenDir);
        if (len > 0.001) {
            screenDir /= len;
            gl_Position.xy += screenDir * 0.007 * gl_Position.w;
        }
    } else {
        gl_Position = ubo.projection * ubo.view * positionWorld;
        if (pushIsSelected() == 6) {
            // 치수선(uv.x=1): 중심선(normal 재해석)→정점 오프셋을 화면 고정 두께로 재스케일.
            // 폴리선처럼 줌 무관 일정 픽셀 → 줌아웃해도 안 얇아짐. 화살표/글자(uv.x=0)는 월드 크기.
            if (uv.x > 0.5) {
                vec3 C = normal;                        // 중심선(월드)
                vec3 O = positionWorld.xyz - C;         // 오프셋(월드 반두께)
                float lenO = length(O);
                if (lenO > 1e-8) {
                    vec4 cClip = ubo.projection * ubo.view * vec4(C, 1.0);
                    float wC = max(cClip.w, 1e-4);
                    float ndcHalf = lenO * abs(ubo.projection[1][1]) / wC;
                    float target = 0.004;               // 화면 높이 ~0.4% (≈2-3px 반두께)
                    float factor = max(1.0, target / max(ndcHalf, 1e-6));
                    gl_Position = ubo.projection * ubo.view * vec4(C + O * factor, 1.0);
                }
            }
            // 코플래너 z-fighting 방지 — 카메라 쪽 미세 바이어스 (near=0 방향).
            gl_Position.z -= 1e-4 * gl_Position.w;
        }
    }

    fragNormalWorld = normalize(pushNormalMatrix() * normal);
    fragPosWorld = positionWorld.xyz;
    fragColor = color;
    fragTexCoord = uv;
}