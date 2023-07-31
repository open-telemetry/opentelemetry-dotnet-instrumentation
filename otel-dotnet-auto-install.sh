#!/bin/sh
set -e

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

case "$OS_TYPE" in
  "linux-glibc"|"linux-musl"|"macos"|"windows")
    ;;
  *)
    echo "Set the operating system type using the OS_TYPE environment variable. Supported values: linux-glibc, linux-musl, macos, windows." >&2
    exit 1
    ;;
esac

test -z "$OTEL_DOTNET_AUTO_HOME" && OTEL_DOTNET_AUTO_HOME="$HOME/.otel-dotnet-auto"
test -z "$TMPDIR" && TMPDIR="$(mktemp -d)"
test -z "$VERSION" && VERSION="v1.0.0-rc.2"

RELEASES_URL="https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases"
ARCHIVE="opentelemetry-dotnet-instrumentation-$OS_TYPE.zip"

TMPFILE="$TMPDIR/$ARCHIVE"
(
  cd "$TMPDIR"
  echo "Downloading $VERSION for $OS_TYPE..."
  curl -sSfLo "$TMPFILE" "$RELEASES_URL/download/$VERSION/$ARCHIVE"
)
rm -rf "$OTEL_DOTNET_AUTO_HOME"
unzip -q "$TMPFILE" -d "$OTEL_DOTNET_AUTO_HOME" 
