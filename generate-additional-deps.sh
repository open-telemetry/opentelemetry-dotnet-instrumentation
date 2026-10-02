#!/bin/sh

# Copyright The OpenTelemetry Authors
# SPDX-License-Identifier: Apache-2.0

set -eu

usage() {
  cat <<'EOF'
Usage: generate-additional-deps.sh --output <directory>

Creates DOTNET_ADDITIONAL_DEPS and DOTNET_SHARED_STORE directory structures
using assemblies from this OpenTelemetry .NET AutoInstrumentation installation.
EOF
}

output_path=''
while [ "$#" -gt 0 ]; do
  case "$1" in
    --output)
      if [ "$#" -lt 2 ]; then
        echo 'Missing value for --output.' >&2
        usage >&2
        exit 1
      fi
      output_path=$2
      shift 2
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [ -z "$output_path" ]; then
  echo 'The --output argument is required.' >&2
  usage >&2
  exit 1
fi

script_dir=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
copy_plan_path="$script_dir/AdditionalDeps/shared-store-copy-plan.txt"
source_additional_deps="$script_dir/AdditionalDeps"

if [ ! -f "$copy_plan_path" ]; then
  echo "AdditionalDeps copy plan was not found: $copy_plan_path" >&2
  exit 1
fi

if [ ! -d "$source_additional_deps/shared" ]; then
  echo "AdditionalDeps dependency contexts were not found: $source_additional_deps/shared" >&2
  exit 1
fi

mkdir -p "$output_path"
output_path=$(CDPATH='' cd -- "$output_path" && pwd)
destination_additional_deps="$output_path/AdditionalDeps"
destination_store="$output_path/store"

mkdir -p "$destination_additional_deps"
if [ "$source_additional_deps" != "$destination_additional_deps" ]; then
  mkdir -p "$destination_additional_deps/shared"
  cp -R "$source_additional_deps/shared/." "$destination_additional_deps/shared/"
fi

while IFS='|' read -r source_relative_path store_relative_path extra_field; do
  if [ -z "$source_relative_path" ] || [ -z "$store_relative_path" ] || [ -n "$extra_field" ]; then
    echo 'Invalid entry in AdditionalDeps copy plan.' >&2
    exit 1
  fi

  for path_part in "$source_relative_path" "$store_relative_path"; do
    case "$path_part" in
      /*|..|../*|*/..|*/../*)
        echo 'AdditionalDeps copy plan contains an unsafe path.' >&2
        exit 1
        ;;
    esac
  done

  source_path="$script_dir/$source_relative_path"
  if [ ! -f "$source_path" ]; then
    echo "AdditionalDeps source file was not found: $source_path" >&2
    exit 1
  fi

  destination_path="$destination_store/$store_relative_path"
  mkdir -p "$(dirname -- "$destination_path")"
  cp "$source_path" "$destination_path"
done < "$copy_plan_path"

cat <<EOF
AdditionalDeps files were generated successfully.

Configure the instrumented application with:
DOTNET_ADDITIONAL_DEPS=$destination_additional_deps
DOTNET_SHARED_STORE=$destination_store

If assembly redirection must be disabled, also configure:
OTEL_DOTNET_AUTO_REDIRECT_ENABLED=false
EOF
