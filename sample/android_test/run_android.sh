#!/bin/bash
# run_android.sh — 안드로이드 에뮬레이터 실행 + 앱 빌드·설치·실행 한 방에.
#
# 하는 일:
#   1. 에뮬레이터가 안 떠 있으면 AVD 실행 후 부팅 대기 (-gpu host: Vulkan 렌더용)
#   2. gradlew 로 APK 빌드 + 설치 (JAVA_HOME 은 Android Studio 번들 JDK 자동 사용)
#   3. 액티비티 실행
#
# 사용:
#   ./run_android.sh              # 기본 AVD 로 실행
#   ./run_android.sh Pixel_7      # AVD 이름 지정
#   ./run_android.sh --list       # 사용 가능한 AVD 목록만 출력
#
# 전제: Android SDK/NDK/CMake 설치 (런타임 에셋은 레포의 sdk/ 에서 가져온다)

set -e
cd "$(dirname "$0")"

SDK="${ANDROID_HOME:-$HOME/Library/Android/sdk}"
EMU="$SDK/emulator/emulator"
ADB="$SDK/platform-tools/adb"
PKG="com.vulkancad.androidtest"
ACT="$PKG/.MainActivity"

# gradlew 는 JDK 필요 — JAVA_HOME 없으면 Android Studio 번들 JBR 사용.
if [ -z "$JAVA_HOME" ]; then
    JBR="/Applications/Android Studio.app/Contents/jbr/Contents/Home"
    [ -d "$JBR" ] && export JAVA_HOME="$JBR"
fi

[ -x "$EMU" ] || { echo "❌ emulator 없음: $EMU (Android SDK 설치 확인)"; exit 1; }

if [ "$1" = "--list" ]; then
    echo "사용 가능한 AVD:"; "$EMU" -list-avds; exit 0
fi

# 실행할 AVD 결정 (인자 > 첫 번째 AVD).
AVD="${1:-$("$EMU" -list-avds | head -1)}"
[ -n "$AVD" ] || { echo "❌ AVD 가 없음. Android Studio > Device Manager 에서 하나 만드세요."; exit 1; }

# 이미 붙은 기기(에뮬레이터/실기) 없으면 에뮬레이터 실행.
if ! "$ADB" get-state >/dev/null 2>&1; then
    echo "===== 에뮬레이터 실행: $AVD (-gpu host) ====="
    "$EMU" -avd "$AVD" -gpu host -no-snapshot-load >/dev/null 2>&1 &
    echo "부팅 대기 중..."
    "$ADB" wait-for-device
    until [ "$("$ADB" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = "1" ]; do sleep 3; done
    echo "부팅 완료."
else
    echo "===== 이미 연결된 기기 사용 ====="
fi

echo "===== 빌드 + 설치 ====="
sh ./gradlew installDebug

echo "===== 앱 실행: $ACT ====="
"$ADB" shell am start -n "$ACT"
echo "✅ 완료 — 에뮬레이터 화면 확인."
echo "   조작: 좌 드래그=이동(pan) · [회전] 버튼 켜면 좌 드래그=궤도회전 · ⌘+드래그=핀치 줌 · 클릭=선택"
echo "   (에뮬레이터는 마우스 장치가 없어 우클릭·휠이 앱에 안 옴 — 실기/블루투스 마우스에선 우=회전, 휠=줌)"
