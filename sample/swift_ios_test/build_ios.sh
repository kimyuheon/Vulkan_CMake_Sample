#!/usr/bin/env bash
# build_ios.sh (iOS 샘플 폴더용 래퍼) — 엔진 루트의 build_ios.sh 를 그대로 호출한다.
#
# iOS 작업은 Xcode 프로젝트가 있는 이 폴더에서 하게 되므로, 엔진 루트까지 올라가지 않고
# 여기서 바로 정적 라이브러리를 빌드할 수 있게 둔 것이다.
#
# ⚠️ 실제 구현은 엔진 루트 `3dEngine/build_ios.sh` 하나뿐이다(로직 중복 방지).
#    빌드 옵션(SDK/아키텍처/배포타겟)을 바꿀 일이 있으면 그쪽을 고칠 것.
#
# 사용:
#   ./build_ios.sh         # 시뮬레이터 + 실기 양쪽
#   ./build_ios.sh sim     # 시뮬레이터만  → build-ios-sim
#   ./build_ios.sh device  # 실기만        → build-ios-device
#
# 빌드 끝나면 Xcode 에서 Clean (Cmd+Shift+K) → Run (Cmd+R).

set -e
cd "$(dirname "$0")/../.."      # samples/swift_ios_test → 엔진 루트(3dEngine)
exec ./build_ios.sh "$@"
