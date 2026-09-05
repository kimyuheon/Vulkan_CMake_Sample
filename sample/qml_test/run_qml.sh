#!/usr/bin/env bash
# 선택 사항: Ubuntu 터미널에서 configure/build/run을 한 번에 수행한다.
# Qt Creator 사용 시 이 스크립트는 필요 없다.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ENGINE_BUILD="${VULKANCAD_ENGINE_BUILD:-$(cd "$SCRIPT_DIR/../../build" && pwd)}"

cmake -S "$SCRIPT_DIR" -B "$SCRIPT_DIR/build" \
    -DVULKANCAD_ENGINE_BUILD="$ENGINE_BUILD"
cmake --build "$SCRIPT_DIR/build"

exec "$SCRIPT_DIR/build/qml_host"
