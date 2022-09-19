#!/bin/sh
set -e

test -z "$VERSION" && {
	echo "Please specify the version by setting the VERSION env var." >&2
	exit 1
}
test -z "$FLAVOUR" && {
  echo "Please specify the flavour by setting the FLAVOUR env var." >&2
  echo "Supported values:" >&2
  echo "    linux-glibc" >&2
  echo "    linux-musl" >&2
  echo "    macos" >&2
  echo "    windows" >&2
	exit 1
}
test -z "$INSTALL_DIR" && INSTALL_DIR="./otel-dotnet-auto"
test -z "$RELEASES_URL" && RELEASES_URL="https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases"
test -z "$TMPDIR" && TMPDIR="$(mktemp -d)"

ARCHIVE="opentelemetry-dotnet-instrumentation-$FLAVOUR.zip"
TMPFILE="$TMPDIR/$ARCHIVE"

(
  cd "$TMPDIR"
  echo "Downloading $VERSION for $FLAVOUR..."
  curl -sSfLo "$TMPFILE" "$RELEASES_URL/download/$VERSION/$ARCHIVE"
)

rm -rf "$INSTALL_DIR"
unzip -q "$TMPFILE" -d "$INSTALL_DIR" 
