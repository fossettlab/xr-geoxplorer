#!/usr/bin/env bash
# Local Unity batchmode wrapper for compile checks and Android builds.
# Editor version is read from ProjectSettings/ProjectVersion.txt.
#
# Usage:
#   ./scripts/unity.sh compile
#   ./scripts/unity.sh build-android
#
# Override the editor binary:
#   UNITY=/path/to/Unity ./scripts/unity.sh compile

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_VERSION="$(grep '^m_EditorVersion:' "${PROJECT_ROOT}/ProjectSettings/ProjectVersion.txt" | awk '{print $2}')"
BUILD_DIR="${PROJECT_ROOT}/build"
LOG_DIR="${PROJECT_ROOT}/Logs"
MAIN_SCENE="Assets/Scenes/GeoXShared.unity"
BUILD_METHOD="GeoXEditor.CommandLineBuild.BuildAndroid"

usage() {
    cat <<EOF
Usage: $(basename "$0") <command>

Commands:
  compile        Import the project and compile scripts (batchmode, no player build)
  build-android  Build the Quest/Android APK to build/GeoXplorer.apk

Environment:
  UNITY          Path to the Unity editor executable (auto-detected if unset)
  UNITY_LOG      Log file path (default: Logs/unity-<command>.log under repo root)

Examples:
  $(basename "$0") compile
  UNITY="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity" \\
    $(basename "$0") build-android
EOF
}

find_unity() {
    if [[ -n "${UNITY:-}" && -x "${UNITY}" ]]; then
        echo "${UNITY}"
        return 0
    fi

    local candidates=(
        "/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
        "${HOME}/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity"
        "/opt/unity/Editor/Unity"
        "unity-editor"
    )

    for candidate in "${candidates[@]}"; do
        if command -v "${candidate}" >/dev/null 2>&1; then
            command -v "${candidate}"
            return 0
        fi
        if [[ -x "${candidate}" ]]; then
            echo "${candidate}"
            return 0
        fi
    done

    echo "Could not find Unity ${UNITY_VERSION}." >&2
    echo "Install via Unity Hub or set UNITY to the editor binary." >&2
    return 1
}

run_unity() {
    local command_name="$1"
    shift

    local unity_bin
    unity_bin="$(find_unity)"

    mkdir -p "${LOG_DIR}" "${BUILD_DIR}"
    local log_file="${UNITY_LOG:-${LOG_DIR}/unity-${command_name}.log}"

    echo "Unity:   ${unity_bin}"
    echo "Project: ${PROJECT_ROOT}"
    echo "Log:     ${log_file}"

    "${unity_bin}" \
        -batchmode \
        -nographics \
        -quit \
        -projectPath "${PROJECT_ROOT}" \
        -logFile "${log_file}" \
        "$@"

    local exit_code=$?
    if [[ ${exit_code} -ne 0 ]]; then
        echo "Unity exited with status ${exit_code}. See ${log_file}" >&2
        return "${exit_code}"
    fi

    echo "Done. Log: ${log_file}"
}

compile() {
    run_unity compile
}

build_android() {
    run_unity build-android \
        -executeMethod "${BUILD_METHOD}" \
        -buildTarget Android
}

main() {
    if [[ $# -lt 1 ]]; then
        usage
        exit 1
    fi

    case "$1" in
        compile)
            compile
            ;;
        build-android)
            build_android
            ;;
        -h|--help|help)
            usage
            ;;
        *)
            echo "Unknown command: $1" >&2
            usage
            exit 1
            ;;
    esac
}

main "$@"
