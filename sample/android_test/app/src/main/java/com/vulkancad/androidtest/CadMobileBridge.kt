package com.vulkancad.androidtest

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.net.Uri
import java.io.ByteArrayOutputStream
import java.nio.ByteBuffer

// 안드로이드 전용 다리 — OS 에만 있는 기능을 엔진에 이어 준다.
//
// 왜 여기서 하나: 엔진(C++)은 안드로이드 클립보드·사진 앱·ML Kit 을 직접 못 만진다.
// 전부 Kotlin API 에만 있다. 그래서 **호스트가 하고 결과를 엔진에 넘기는** 모양으로
// 뒤집는다. iOS 도 같은 모양이다(UIPasteboard / Vision).
//
// OCR 로 Tesseract 를 안 쓰는 이유: 45MB 를 앱에 넣어야 하는데, ML Kit 이 무료이고
// 한글·혼합 글자에서 더 정확하다.
//
// ⚠️ ML Kit 을 쓰려면 app/build.gradle 에 다음이 필요하다:
//      implementation("com.google.mlkit:text-recognition-korean:16.0.1")
//    (라이브러리를 안 넣었으면 recognizeText 는 그냥 아무것도 안 한다 — 빌드는 된다)
object CadMobileBridge {

    // ── 클립보드 ──

    /** 앱 시작 후 한 번. 이후 엔진이 복사하면 pumpClipboard 가 OS 클립보드로 옮긴다. */
    fun install() = CadNative.nativeInstallClipboard()

    /**
     * 매 프레임(또는 주기적으로) 부른다.
     *
     * 엔진 → Kotlin 방향을 JNI 콜백으로 하면 스레드·전역참조 관리가 번거로워,
     * 네이티브에 담아 두고 여기서 꺼내 가는 방식으로 뒀다.
     */
    fun pumpClipboard(ctx: Context) {
        val copied = CadNative.nativeTakeCopiedText() ?: return
        val cm = ctx.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
        cm.setPrimaryClip(ClipData.newPlainText("VulkanCAD", copied))
    }

    /** 붙여넣기 버튼에서. 엔진이 물어보지 않고 **호스트가 민다**. */
    fun paste(ctx: Context) {
        val cm = ctx.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
        val text = cm.primaryClip?.getItemAt(0)?.coerceToText(ctx)?.toString() ?: return
        if (text.isNotEmpty()) CadNative.nativePasteText(text)
    }

    // ── 이미지 ──

    /** 사진 선택(Intent)에서 받은 Uri 를 도면 밑에 깐다. 반환: 객체 id (실패 0). */
    fun attachImage(ctx: Context, uri: Uri): Int {
        val bmp = ctx.contentResolver.openInputStream(uri)?.use {
            BitmapFactory.decodeStream(it)
        } ?: return 0
        return attachImage(bmp)
    }

    fun attachImage(bitmap: Bitmap): Int {
        val rgba = toRgba(bitmap) ?: return 0
        return CadNative.nativeAttachImageRgba(rgba, bitmap.width, bitmap.height)
    }

    /**
     * Bitmap → RGBA8.
     *
     * ⚠️ ARGB_8888 로 맞춰야 한다. Bitmap 이 RGB_565 등 다른 형식이면 바이트 배치가
     *    달라 색이 깨진다. copy 로 한 번 통일한다.
     */
    private fun toRgba(bitmap: Bitmap): ByteArray? {
        val src = if (bitmap.config == Bitmap.Config.ARGB_8888) bitmap
                  else bitmap.copy(Bitmap.Config.ARGB_8888, false) ?: return null
        val buf = ByteBuffer.allocate(src.width * src.height * 4)
        src.copyPixelsToBuffer(buf)
        return buf.array()
    }

    // ── OCR (ML Kit) ──

    /**
     * 붙여 놓은 이미지에서 글자를 읽어 엔진에 넘긴다.
     *
     * 넘긴 뒤의 흐름(줄마다 네모 → 드래그로 고르기 → 커서 배치)은 **데스크톱과 같다**.
     * 엔진 호출은 메인 스레드에서 한다 — 엔진은 한 스레드 전제다.
     *
     * ML Kit 의존을 안 넣었으면 이 함수는 조용히 아무것도 하지 않는다(리플렉션으로 확인).
     */
    fun recognizeText(bitmap: Bitmap, imageId: Int, onDone: (Int) -> Unit = {}) {
        val recognizer = try {
            val opts = Class.forName("com.google.mlkit.vision.text.korean.KoreanTextRecognizerOptions\$Builder")
                .getDeclaredConstructor().newInstance()
            val built = opts.javaClass.getMethod("build").invoke(opts)
            val cls = Class.forName("com.google.mlkit.vision.text.TextRecognition")
            cls.getMethod("getClient", Class.forName("com.google.mlkit.vision.text.TextRecognizerOptionsInterface"))
                .invoke(null, built)
        } catch (e: Throwable) {
            // 의존을 안 넣은 빌드 — 기능만 없고 앱은 정상 동작한다.
            onDone(0)
            return
        }

        try {
            val inputImageCls = Class.forName("com.google.mlkit.vision.common.InputImage")
            val image = inputImageCls
                .getMethod("fromBitmap", Bitmap::class.java, Int::class.javaPrimitiveType)
                .invoke(null, bitmap, 0)

            val task = recognizer.javaClass.getMethod("process", inputImageCls).invoke(recognizer, image)
            val addOnSuccess = task!!.javaClass.methods.first {
                it.name == "addOnSuccessListener" && it.parameterTypes.size == 1
            }
            val listenerCls = Class.forName("com.google.android.gms.tasks.OnSuccessListener")
            val proxy = java.lang.reflect.Proxy.newProxyInstance(
                listenerCls.classLoader, arrayOf(listenerCls)
            ) { _, _, args ->
                pushLines(args?.getOrNull(0), imageId, onDone)
                null
            }
            addOnSuccess.invoke(task, proxy)
        } catch (e: Throwable) {
            onDone(0)
        }
    }

    /** ML Kit Text 결과 → 엔진. 줄 단위로, 읽는 순서 그대로 넘긴다. */
    private fun pushLines(text: Any?, imageId: Int, onDone: (Int) -> Unit) {
        if (text == null) { onDone(0); return }
        var count = 0
        try {
            CadNative.nativeBeginOcrLines(imageId)
            val blocks = text.javaClass.getMethod("getTextBlocks").invoke(text) as List<*>
            for (b in blocks) {
                val lines = b!!.javaClass.getMethod("getLines").invoke(b) as List<*>
                for (l in lines) {
                    val s = l!!.javaClass.getMethod("getText").invoke(l) as String
                    val box = l.javaClass.getMethod("getBoundingBox").invoke(l)
                    if (s.isEmpty() || box == null) continue
                    // Rect 는 이미 **이미지 픽셀 좌표(좌상단 원점)** 라 변환이 필요 없다.
                    // (iOS Vision 은 정규화 + y 가 위로라 그쪽에서만 뒤집는다.)
                    val r = box as android.graphics.Rect
                    CadNative.nativeAddOcrLine(s, r.left, r.top, r.right, r.bottom)
                    count++
                }
            }
        } catch (e: Throwable) {
            // 부분 실패해도 지금까지 넣은 건 살린다
        } finally {
            CadNative.nativeEndOcrLines()
            onDone(count)
        }
    }
}
