# Native dependencies

## fmt

* Source: <https://github.com/fmtlib/fmt/>
* Version: 10.1.1

### Setup

Package is acquired via Github release.

1. Download source code (zip) from release
1. Copy content of main folder, `src`, `include`, and `support` to `opentelemetry-dotnet-instrumentation\src\OpenTelemetry.AutoInstrumentation.Native\lib\fmt`
1. Resync `src` files and references in `opentelemetry-dotnet-instrumentation\src\OpenTelemetry.AutoInstrumentation.Native\OpenTelemetry.AutoInstrumentation.Native.vcxproj`

## spdlog

* Source: <https://github.com/gabime/spdlog>
* Version: 1.12.0

### Setup

Package is acquired via Github release.

1. Download source code (zip) from release
1. Copy `src` and `include` to `opentelemetry-dotnet-instrumentation\src\OpenTelemetry.AutoInstrumentation.Native\lib\spdlog`
1. Resync `src` files and references in `opentelemetry-dotnet-instrumentation\src\OpenTelemetry.AutoInstrumentation.Native\OpenTelemetry.AutoInstrumentation.Native.vcxproj`
