package com.lotcad.androidtest

import android.content.Context
import java.io.File

// APK assets(models/fonts/textures) → filesDir 로 추출하고 엔진에 그 경로를 알려 준다.
// 엔진은 상대경로 fopen 을 쓰고, 생성될 때 작업 폴더를 이 경로로 옮긴다.
// 앱마다 onCreate 에서 엔진을 만들기(Surface 생성) **전에** 한 번 부른다.
object EngineAssets {
    fun install(context: Context) {
        val root = context.filesDir
        for (dir in listOf("models", "fonts", "textures")) copyAssetDir(context, dir, root)
        CadNative.nativeSetAssetPath(root.absolutePath)
    }

    private fun copyAssetDir(context: Context, dir: String, root: File) {
        val items = context.assets.list(dir) ?: return
        File(root, dir).mkdirs()
        for (item in items) {
            val path = "$dir/$item"
            val children = context.assets.list(path)
            if (children != null && children.isNotEmpty()) {
                copyAssetDir(context, path, root)  // 하위 폴더
            } else {
                context.assets.open(path).use { input ->
                    File(root, path).outputStream().use { input.copyTo(it) }
                }
            }
        }
    }
}
