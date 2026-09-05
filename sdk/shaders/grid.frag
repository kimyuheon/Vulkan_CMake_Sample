#version 450

// 무한 그리드 (뷰 타입별 평면 선택, CAD Z-up 관례)
// - planeType 0 = XY 평면 (Z=0)  : Top / Isometric  (바닥)
// - planeType 1 = XZ 평면 (Y=0)  : Front
// - planeType 2 = YZ 평면 (X=0)  : Right

layout(location = 0) in vec3 nearPoint;
layout(location = 1) in vec3 farPoint;
layout(location = 0) out vec4 outColor;

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
    vec3  lineColor;
    vec3  xAxisColor;
    vec3  yAxisColor;
    vec3  zAxisColor;
    float fadeStart;
    float fadeEnd;
    int   planeType;
    float baseSpacing;   // 세밀 그리드 셀 크기 (월드 단위). 줌에 따라 10배 스냅
    vec2  fadeCenter;    // 페이드 중심 (평면 2D) = 카메라 타겟. 원점 아님 → 그리드가 뷰를 따라감
} push;

float computeDepth(vec3 pos) {
    vec4 clip = ubo.projection * ubo.view * vec4(pos, 1.0);
    return clip.z / clip.w;
}

// 그리드 평면 위에서 target(원점)으로부터의 거리
// Ortho 모드의 인위적 카메라 거리(250+)에 영향받지 않음
float planarDistance(vec2 planarCoord) {
    return length(planarCoord);
}

// 2D 좌표를 받아 그리드 라인 픽셀 (축 강조는 main() 에서 별도 오버레이로 처리)
// grazing angle 에서 derivative 가 너무 커지면 alpha 를 감쇠해 모아레 방지
vec4 grid2D(vec2 coord) {
    vec2 derivative = fwidth(coord);
    vec2 g = abs(fract(coord - 0.5) - 0.5) / derivative;
    float line = min(g.x, g.y);

    // grazing angle 감쇠: derivative 가 5 이상이면 라인이 뭉개져서 감쇠 시작
    float grazeFade = clamp(1.0 - (max(derivative.x, derivative.y) - 5.0) * 0.1, 0.0, 1.0);

    // 그리드 라인 색: push 상수로 외부 설정 (기본: 배경과 근접한 어두운 회색)
    return vec4(push.lineColor, (1.0 - min(line, 1.0)) * grazeFade);
}

void main() {
    vec3 fragPos3D;
    vec2 coordSmall, coordBig;
    vec3 axisUColor, axisVColor;
    float worldU, worldV;

    // 축 색상: 배경과 톤 맞추기 위해 채도 낮춤
    if (push.planeType == 1) {
        // XZ 평면 (Y=0) — Front
        float t = -nearPoint.y / (farPoint.y - nearPoint.y);
        if (t < 0.0) discard;
        fragPos3D = nearPoint + t * (farPoint - nearPoint);
        coordSmall = fragPos3D.xz;
        worldU = fragPos3D.x;
        worldV = fragPos3D.z;
        axisUColor = push.zAxisColor; // x=0 선 → Z축 색
        axisVColor = push.xAxisColor; // z=0 선 → X축 색
    } else if (push.planeType == 2) {
        // YZ 평면 (X=0) — Right
        float t = -nearPoint.x / (farPoint.x - nearPoint.x);
        if (t < 0.0) discard;
        fragPos3D = nearPoint + t * (farPoint - nearPoint);
        coordSmall = fragPos3D.yz;
        worldU = fragPos3D.y;
        worldV = fragPos3D.z;
        axisUColor = push.zAxisColor; // y=0 선 → Z축 색
        axisVColor = push.yAxisColor; // z=0 선 → Y축 색
    } else {
        // XY 평면 (Z=0) — Top / Isometric (CAD 바닥)
        float t = -nearPoint.z / (farPoint.z - nearPoint.z);
        if (t < 0.0) discard;
        fragPos3D = nearPoint + t * (farPoint - nearPoint);
        coordSmall = fragPos3D.xy;
        worldU = fragPos3D.x;
        worldV = fragPos3D.y;
        axisUColor = push.yAxisColor; // x=0 선 → Y축 색
        axisVColor = push.xAxisColor; // y=0 선 → X축 색
    }

    // 계산된 깊이를 [0,1]로 클램프 (far edge 수치오차 방지)
    float depth = computeDepth(fragPos3D);
    gl_FragDepth = clamp(depth, 0.0, 1.0);

    // baseSpacing 단위로 정규화 → 줌에 따라 셀 크기 10배 스냅
    // 2단계 스케일: 세밀(baseSpacing) + 큼(baseSpacing × 10) 겹치기
    float s = max(push.baseSpacing, 0.0001);
    vec4 smallGrid = grid2D(coordSmall / s);
    vec4 bigGrid   = grid2D(coordSmall / (s * 10.0));

    // 페이드: 카메라가 보는 지점(fadeCenter)으로부터의 평면 거리 기준. 월드 원점이 아니라 뷰 타겟
    // 기준이라 팬/줌을 따라 그리드가 항상 화면을 채운다 (원점 고정 원반 → 뷰 추종 원반).
    float dist = length(coordSmall - push.fadeCenter);
    float fading = max(0.0, 1.0 - smoothstep(push.fadeStart, push.fadeEnd, dist));

    outColor = smallGrid + bigGrid;
    outColor.a *= fading;

    // === 축 라인 오버레이 ===
    // 기존 구현은 grid2D 안에서 worldU/V 를 coord 파생 minV (coord 단위) 와 비교해 (1) 셀 크기(s)에
    // 따라 폭이 고무줄이고 (2) 그리드 cell 알파에 종속돼 셀 경계마다만 그려져 "바코드 밴드" 처럼 보였음.
    // 여기선 월드 단위 fwidth 로 픽셀 거리를 계산해 축 독립적으로 1.5픽셀 폭 솔리드 라인을 오버레이.
    float axisPixU = abs(worldU) / max(fwidth(worldU), 1e-6);   // V 축(U=0) 까지의 픽셀 거리
    float axisPixV = abs(worldV) / max(fwidth(worldV), 1e-6);   // U 축(V=0) 까지의 픽셀 거리
    float axisVAlpha = (1.0 - smoothstep(0.5, 1.5, axisPixV)) * fading;
    float axisUAlpha = (1.0 - smoothstep(0.5, 1.5, axisPixU)) * fading;
    if (axisVAlpha > 0.001)
        outColor = vec4(mix(outColor.rgb, axisVColor, axisVAlpha), max(outColor.a, axisVAlpha));
    if (axisUAlpha > 0.001)
        outColor = vec4(mix(outColor.rgb, axisUColor, axisUAlpha), max(outColor.a, axisUAlpha));

    if (outColor.a <= 0.001) discard;
}
