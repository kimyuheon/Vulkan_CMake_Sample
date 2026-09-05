#version 450

// Thick line via quad expansion in vertex shader.
//
// LotModel::Vertex 의 4 attribute 슬롯을 thick line 용도로 재해석:
//   location 0 (position)        = 이 vertex 의 segment endpoint (a 또는 b)
//   location 1 (color)            = 사용 안 함 (push constant 의 color 사용)
//   location 2 (normal)           = segment 의 반대편 endpoint  ← 재해석
//   location 3 (uv)               = (side, endFlag)              ← 재해석
//                                     uv.x: -1.0 또는 +1.0 (perpendicular 방향)
//                                     uv.y:  0.0 (이 vertex 가 start) / 1.0 (end)
//
// 같은 LotModel::Vertex layout 을 쓰므로 별도 binding/attribute 설정 없이 일반 LotModel
// 인프라를 그대로 재사용.

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 inColor;        // 미사용 — layout 호환성 유지
layout(location = 2) in vec3 otherEndpoint;  // segment 반대편 endpoint
layout(location = 3) in vec2 sideFlag;        // x=side(-1/+1), y=endFlag(0/1)

layout(location = 0) out vec3 v_color;

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

layout(push_constant) uniform Push {
    mat4 modelMatrix;
    vec3 color;
    float thicknessPx;
    vec2 viewportSize;
    int isSelected;     // 0=일반(push.color), 1=선택(노란색), 2=호버(하늘색)
} push;

void main() {
    // endFlag 로 자기 자신이 segment 의 어느 끝인지 판정
    float isEnd = sideFlag.y;
    vec3 startW = mix(position, otherEndpoint, isEnd);
    vec3 endW   = mix(otherEndpoint, position, isEnd);

    mat4 vp = ubo.projection * ubo.view;
    vec4 startClip = vp * push.modelMatrix * vec4(startW, 1.0);
    vec4 endClip   = vp * push.modelMatrix * vec4(endW,   1.0);

    // 자기 자신의 clip 위치 (offset 적용 전)
    vec4 thisClip = mix(startClip, endClip, isEnd);

    // 양 끝점이 모두 카메라 뒤(w<=0)면 그리지 않게 강제로 NaN 대체
    if (startClip.w <= 0.0 && endClip.w <= 0.0) {
        gl_Position = vec4(2.0, 2.0, 2.0, 1.0);  // NDC clip out
        v_color = push.color;
        return;
    }

    // 화면 공간 위치 (픽셀 단위) — perspective divide 후 viewport 스케일
    // 한쪽 w<=0 인 경우는 작은 양수로 보정해 segment 방향만 유지 (clip 단계에서 적절히 잘림)
    float wA = max(startClip.w, 1e-4);
    float wB = max(endClip.w,   1e-4);
    vec2 startScreen = (startClip.xy / wA) * 0.5 * push.viewportSize;
    vec2 endScreen   = (endClip.xy   / wB) * 0.5 * push.viewportSize;

    vec2 segDir = endScreen - startScreen;
    float segLen = length(segDir);
    vec2 dir = (segLen > 0.0001) ? segDir / segLen : vec2(1.0, 0.0);

    // 90도 회전 → screen-space perpendicular
    vec2 perp = vec2(-dir.y, dir.x);

    // 픽셀 단위 offset 을 NDC 단위로 변환 후 perspective-correct (×w)
    // ndc_offset = (2 * pixel) / viewport
    vec2 ndcOffset = (perp * push.thicknessPx * 0.5 * sideFlag.x * 2.0) / push.viewportSize;
    thisClip.xy += ndcOffset * thisClip.w;

    // 사각 캡 — 끝점을 세그먼트 방향으로 두께 절반만큼 연장. 줌하면 세그먼트가 화면에서
    // 짧아져(서브픽셀 길이) quad(폭×0)가 픽셀을 못 덮어 끊기고, 조인트에서도 틈이 벌어짐.
    // 캡으로 최소 길이를 보장해 선이 끊기지 않게 함. end=+dir, start=-dir.
    float capSign = (isEnd > 0.5) ? 1.0 : -1.0;
    vec2 ndcCap = (dir * push.thicknessPx * 0.5 * capSign * 2.0) / push.viewportSize;
    thisClip.xy += ndcCap * thisClip.w;

    // 화면공간 두께 확장은 xy 만 옮기고 z 는 중심선 깊이를 그대로 둔다. 비스듬한 뷰에서는
    // 확장된 폭 픽셀이 바닥면보다 뒤 깊이가 되어 면에 가려 → 선이 점선처럼 끊김.
    // 카메라 쪽으로 미세 바이어스(ZERO_TO_ONE: near=0)해 코플래너/근접 면을 일관되게 이김.
    thisClip.z -= 1e-4 * thisClip.w;

    gl_Position = thisClip;

    // 선 계열은 feature edge 가 없어 엣지 패스를 못 탄다 → 선 색 자체를 바꿔 강조한다.
    // 선택(노랑)이 호버(하늘색)보다 우선 — 호출자가 그 순서로 값을 정한다.
    v_color = (push.isSelected == 1) ? vec3(1.0, 0.85, 0.0)
            : (push.isSelected == 2) ? vec3(0.45, 0.80, 1.0)
                                     : push.color;
}
