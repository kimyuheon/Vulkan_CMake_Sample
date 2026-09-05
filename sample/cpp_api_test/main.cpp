#include "../../sdk/include/VulkanCAD_API.h"

#include <chrono>
#include <cstdint>
#include <iostream>
#include <thread>

int main() {
    std::cout << "[cpp_api_test] create engine" << std::endl;
    if (!CAD_CreateEngine()) {
        std::cerr << "[cpp_api_test] CAD_CreateEngine failed" << std::endl;
        return 1;
    }
    const uint32_t boxId = CAD_CreateBox(0.0f, 0.0f, 1.0f, 0.5f, 0.5f, 0.5f);
    if (boxId == 0) {
        std::cerr << "[cpp_api_test] CAD_CreateBox failed" << std::endl;
        CAD_DestroyEngine();
        return 1;
    }

    CAD_SetPosition(boxId, 1.0f, 0.0f, 1.5f);
    CAD_SetScale(boxId, 0.8f, 0.8f, 0.8f);
    CAD_SetRotationAxisAngle(boxId, 0.0f, 0.0f, 1.0f, 0.7853982f);
    CAD_SelectObject(boxId, false);
    CAD_RequestZoomExtents();

    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;
    if (CAD_GetPosition(boxId, &x, &y, &z)) {
        std::cout << "[cpp_api_test] box position: "
                  << x << ", " << y << ", " << z << std::endl;
    }

    std::cout << "[cpp_api_test] object count: "
              << CAD_GetObjectCount() << std::endl;
    std::cout << "[cpp_api_test] selected count: "
              << CAD_GetSelectedCount() << std::endl;

    // ── 편집 명령 예시 ──
    // 배열 — 선택(박스)을 가로 3 × 세로 2 로 복제. 평면은 활성 뷰에서 정해진다.
    if (CAD_ArrayRect(3, 2, 1.5f, 1.5f))
        std::cout << "[cpp_api_test] array -> count: " << CAD_GetObjectCount() << std::endl;

    // 분해 — 문자를 윤곽선 폴리선으로 쪼갠다.
    // (폴리선·사각형을 선택하면 개별 선분으로 쪼개진다. 선·원·호는 대상이 아니다)
    const uint32_t textId = CAD_CreateText(0.0f, -2.0f, 0.0f, "CAD", 300.0f);
    if (textId != 0) {
        CAD_SelectObject(textId, /*additive*/false);
        const uint32_t before = CAD_GetObjectCount();
        if (CAD_Explode(/*keepOriginal*/false)) {
            std::cout << "[cpp_api_test] explode: " << before
                      << " -> " << CAD_GetObjectCount() << std::endl;
            CAD_Undo();     // 한 스텝이면 통째로 복원된다
            std::cout << "[cpp_api_test] after undo: " << CAD_GetObjectCount() << std::endl;
        }
    }
    CAD_RequestZoomExtents();

    while (!CAD_ShouldClose()) {
        if (!CAD_Tick()) break;
        std::this_thread::sleep_for(std::chrono::milliseconds(16));
    }

    CAD_DeleteObject(boxId);
    CAD_DestroyEngine();

    std::cout << "[cpp_api_test] done" << std::endl;
    return 0;
}
