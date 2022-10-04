#!/bin/sh
set -e

case "$OS_TYPE" in
  "linux-glibc"|"linux-musl"|"macos"|"windows")
    ;;
  *)
    echo "Please specify the operating system type by setting the OS_TYPE environment variable. Supported values: linux-glibc, linux-musl, macos, windows." >&2
    exit 1
    ;;
esac

test -z "$INSTALL_DIR" && INSTALL_DIR="./otel-dotnet-auto"
test -z "$RELEASES_URL" && RELEASES_URL="https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases"
test -z "$TMPDIR" && TMPDIR="$(mktemp -d)"
test -z "$VERSION" && VERSION="v0.3.1-beta.1"

ARCHIVE="opentelemetry-dotnet-instrumentation-$OS_TYPE.zip"
TMPFILE="$TMPDIR/$ARCHIVE"

(
  cd "$TMPDIR"
  echo "Downloading $VERSION for $OS_TYPE..."
  curl -sSfLo "$TMPFILE" "$RELEASES_URL/download/$VERSION/$ARCHIVE"
)

rm -rf "$INSTALL_DIR"
unzip -q "$TMPFILE" -d "$INSTALL_DIR" 
