#!/bin/sh

SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)
cd $SCRIPT_DIR/..

LC_ALL=C

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

if [ "$OS_TYPE" == "windows"  ]; then
    EXT=.exe
else
    EXT=
fi

# files to format
NATIVE_FILES=$(find . -iname *.cpp -o -iname *.hpp | grep -v ./packages | grep -v ./src/OpenTelemetry.AutoInstrumentation.Native/lib)

# clang-format
echo "$NATIVE_FILES" | xargs "./bin/artifacts/clang-format$EXT" -style=file -i

## check if anything changed
if [ -z "$(git status --porcelain)" ]; then
    exit 1
fi
