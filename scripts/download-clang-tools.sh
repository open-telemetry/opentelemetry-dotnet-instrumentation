#!/bin/bash

set -eo pipefail

SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)

# guess OS_TYPE if not provided
if [ -z "$OS_TYPE" ]; then
  case "$(uname -s | tr '[:upper:]' '[:lower:]')" in
    cygwin_nt*|mingw*|msys_nt*)
      OS_TYPE="windows"
      ;;
    linux*)
      if [ "$(ldd /bin/ls | grep -m1 'musl')" ]; then
        OS_TYPE="linux-musl"
      else
        OS_TYPE="linux-glibc"
      fi
      ;;
    darwin*)
      OS_TYPE="macos"
      ;;
  esac
fi

function DownloadClangTool {
    if [ "$OS_TYPE" = "windows"  ]; then
        FILENAME=$1.exe
    else
        FILENAME=$1
    fi

    case "$OS_TYPE" in
        "linux-glibc")
            TOOLS_URL=https://clrjit2.blob.core.windows.net/clang-tools/17.0.6/linux-x64/$FILENAME
            ;;
        "macos")
            TOOLS_URL=https://clrjit2.blob.core.windows.net/clang-tools/17.0.6/osx-arm64/$FILENAME
            ;;
        "windows")
            TOOLS_URL=https://clrjit2.blob.core.windows.net/clang-tools/17.0.6/windows-x64/$FILENAME
            ;;
        *)
            echo "Set the operating system type using the OS_TYPE environment variable. Supported values: linux-glibc, macos, windows." >&2
            exit 1
            ;;
    esac

    cd $SCRIPT_DIR/..
    mkdir -p bin/artifacts
    cd bin/artifacts

    curl --retry 5 -o "${FILENAME}" "$TOOLS_URL"
    chmod +x $FILENAME
}

DownloadClangTool "clang-format"
# DownloadClangTool "clang-tidy" # unused
