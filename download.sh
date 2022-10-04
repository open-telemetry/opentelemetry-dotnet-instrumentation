#!/bin/sh
set -e

case "$DISTRIBUTION" in
  "linux-glibc"|"linux-musl"|"macos"|"windows")
    ;;
  *)
    echo "Please specify the distribution by setting the DISTRIBUTION env var. Supported values: linux-glibc, linux-musl, macos, windows." >&2
    exit 1
    ;;
esac

test -z "$INSTALL_DIR" && INSTALL_DIR="./otel-dotnet-auto"
test -z "$RELEASES_URL" && RELEASES_URL="https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases"
test -z "$TMPDIR" && TMPDIR="$(mktemp -d)"
test -z "$VERSION" && VERSION="v0.3.1-beta.1"

ARCHIVE="opentelemetry-dotnet-instrumentation-$DISTRIBUTION.zip"
TMPFILE="$TMPDIR/$ARCHIVE"

(
  cd "$TMPDIR"
  echo "Downloading $VERSION for $DISTRIBUTION..."
  curl -sSfLo "$TMPFILE" "$RELEASES_URL/download/$VERSION/$ARCHIVE"
)

rm -rf "$INSTALL_DIR"
unzip -q "$TMPFILE" -d "$INSTALL_DIR" 
