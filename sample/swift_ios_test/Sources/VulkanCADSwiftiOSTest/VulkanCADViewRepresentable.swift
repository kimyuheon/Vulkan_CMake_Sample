import SwiftUI
import UIKit

// SwiftUI 에 UIKit (UIView) 을 박아넣는 어댑터.
// SwiftUI 라이프사이클에서 우리의 VulkanCADMetalView 와 엔진을 연결한다.
struct VulkanCADViewRepresentable: UIViewRepresentable {
    let engine: VulkanCADEngine
    let onReady: (UIView) -> Void

    func makeCoordinator() -> Coordinator {
        Coordinator(engine: engine, onReady: onReady)
    }

    func makeUIView(context: Context) -> VulkanCADMetalView {
        let view = VulkanCADMetalView(frame: .zero)
        view.engine = engine   // 제스처 → 엔진 입력 연결
        context.coordinator.view = view
        // SwiftUI 가 첫 레이아웃 후 size 를 알려줄 때까지 잠깐 기다린 뒤 attach.
        DispatchQueue.main.async {
            context.coordinator.handleInitialAttach()
        }
        return view
    }

    func updateUIView(_ uiView: VulkanCADMetalView, context: Context) {
        // SwiftUI 가 사이즈 변경 등 갱신을 알릴 때마다 엔진에 알림
        context.coordinator.handleResize()
    }

    @MainActor
    final class Coordinator {
        let engine: VulkanCADEngine
        let onReady: (UIView) -> Void
        weak var view: VulkanCADMetalView?
        private var didReady = false

        init(engine: VulkanCADEngine, onReady: @escaping (UIView) -> Void) {
            self.engine = engine
            self.onReady = onReady
        }

        func handleInitialAttach() {
            guard !didReady, let view = view, view.bounds.size.width > 0 else { return }
            didReady = true
            onReady(view)
        }

        func handleResize() {
            guard let view = view, engine.isCreated else { return }
            let scale = view.window?.screen.scale ?? UIScreen.main.scale
            let pixelSize = CGSize(
                width: view.bounds.size.width  * scale,
                height: view.bounds.size.height * scale)
            engine.resize(to: pixelSize)
        }
    }
}
