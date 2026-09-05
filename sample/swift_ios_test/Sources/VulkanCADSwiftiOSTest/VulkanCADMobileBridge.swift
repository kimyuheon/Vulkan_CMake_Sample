// iOS 전용 다리 — OS 에만 있는 기능을 엔진에 이어 준다.
//
// 왜 여기서 하나: 엔진(C++)은 iOS 클립보드·사진 앱·OCR 을 직접 못 만진다. 전부 Swift
// 프레임워크(UIPasteboard / PhotosUI / Vision)에만 있다. 그래서 **호스트가 하고 결과를
// 엔진에 넘기는** 모양으로 뒤집는다. 안드로이드도 같은 모양이다(ClipboardManager / ML Kit).
//
// OCR 로 Tesseract 를 안 쓰는 이유: 45MB 를 앱에 넣어야 하는데, iOS 내장 Vision 이
// **추가 용량 0** 이고 한글·혼합 글자에서 더 정확하다.

import CVulkanCAD
import Foundation
import PhotosUI
import UIKit
import Vision

// MARK: - 클립보드

/// 엔진이 "복사" 할 때 불린다. C 함수 포인터라 문맥을 못 가지므로 전역 함수여야 한다.
private func engineCopyToPasteboard(_ text: UnsafePointer<CChar>?) {
    guard let text else { return }
    UIPasteboard.general.string = String(cString: text)
}

extension VulkanCADEngine {

    /// 앱 시작 후 한 번 부른다.
    func installClipboardBridge() {
        CAD_SetOnCopyText(engineCopyToPasteboard)
    }

    /// 붙여넣기 버튼에서 부른다 — 엔진이 물어보지 않고 **호스트가 민다**.
    func pasteFromPasteboard() {
        guard let s = UIPasteboard.general.string else { return }
        CAD_PasteText(s)
    }

    // MARK: - 이미지 붙이기

    /// 사진 앱에서 고른 이미지를 도면 밑에 깐다.
    ///
    /// ⚠️ PHPicker 는 **경로가 아니라 데이터**를 준다. 그래서 파일로 떨구지 않고
    ///    RGBA 픽셀을 그대로 넘기는 CAD_AttachImageFromMemory 를 쓴다.
    @discardableResult
    func attachImage(_ image: UIImage, widthWorld: Float = 0.0) -> UInt32 {
        guard let rgba = Self.rgbaBytes(from: image) else { return 0 }
        return rgba.pixels.withUnsafeBufferPointer { buf in
            CAD_AttachImageFromMemory(buf.baseAddress, Int32(rgba.width), Int32(rgba.height),
                                      "사진", widthWorld, 0, 0, 0)
        }
    }

    /// UIImage → RGBA8. Vision 과 엔진이 **같은 픽셀 좌표**를 쓰도록 여기서 한 번만 만든다.
    static func rgbaBytes(from image: UIImage) -> (pixels: [UInt8], width: Int, height: Int)? {
        guard let cg = image.cgImage else { return nil }
        let w = cg.width, h = cg.height
        var pixels = [UInt8](repeating: 0, count: w * h * 4)
        let space = CGColorSpaceCreateDeviceRGB()
        guard let ctx = CGContext(
            data: &pixels, width: w, height: h,
            bitsPerComponent: 8, bytesPerRow: w * 4, space: space,
            bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else { return nil }
        ctx.draw(cg, in: CGRect(x: 0, y: 0, width: w, height: h))
        return (pixels, w, h)
    }

    // MARK: - OCR (Vision)

    /// 붙여 놓은 이미지에서 글자를 읽어 엔진에 넘긴다.
    ///
    /// 넘긴 뒤의 흐름(줄마다 네모 → 드래그로 고르기 → 커서 배치)은 **데스크톱과 같다**.
    /// 인식은 백그라운드에서 하고, 엔진 호출만 메인으로 되돌린다 — 엔진은 한 스레드 전제다.
    func recognizeText(in image: UIImage, imageId: UInt32,
                       languages: [String] = ["ko-KR", "en-US"]) {
        guard let cg = image.cgImage else { return }
        let pixelH = CGFloat(cg.height)
        let pixelW = CGFloat(cg.width)

        let request = VNRecognizeTextRequest { request, _ in
            let observations = (request.results as? [VNRecognizedTextObservation]) ?? []

            // 읽는 순서로 정렬 — 위에서 아래, 같은 줄이면 왼쪽부터.
            // "시작~끝 사이 전부" 선택이 이 순서를 따르므로 여기서 맞춰 둬야 한다.
            let sorted = observations.sorted { a, b in
                if abs(a.boundingBox.midY - b.boundingBox.midY) > 0.01 {
                    return a.boundingBox.midY > b.boundingBox.midY   // Vision 은 y 가 위로 증가
                }
                return a.boundingBox.minX < b.boundingBox.minX
            }

            DispatchQueue.main.async {
                CAD_BeginOcrLines(imageId)
                for ob in sorted {
                    guard let text = ob.topCandidates(1).first?.string, !text.isEmpty else { continue }
                    // ⚠️ Vision 은 **정규화 좌표(0~1, y 가 위로)**, 엔진은 **픽셀(y 가 아래로)**.
                    //    여기서 안 바꾸면 글자 네모가 위아래로 뒤집혀 붙는다.
                    let b = ob.boundingBox
                    let x0 = Int(b.minX * pixelW)
                    let x1 = Int(b.maxX * pixelW)
                    let y0 = Int((1.0 - b.maxY) * pixelH)
                    let y1 = Int((1.0 - b.minY) * pixelH)
                    CAD_AddOcrLine(text, Int32(x0), Int32(y0), Int32(x1), Int32(y1))
                }
                CAD_EndOcrLines()
            }
        }
        request.recognitionLevel = .accurate
        request.recognitionLanguages = languages
        request.usesLanguageCorrection = true

        DispatchQueue.global(qos: .userInitiated).async {
            try? VNImageRequestHandler(cgImage: cg, options: [:]).perform([request])
        }
    }

    /// 사진 고르기 → 붙이기 → 글자 인식까지 한 번에.
    func attachImageAndRecognize(_ image: UIImage) {
        let id = attachImage(image)
        guard id != 0 else { return }
        recognizeText(in: image, imageId: id)
    }
}
