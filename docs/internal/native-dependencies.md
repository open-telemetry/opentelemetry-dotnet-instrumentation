# Native dependencies

## fmt

* Source: <https://github.com/fmtlib/fmt/>
* Version: 10.1.1

### Setup

Package is acquired via [Microsoft vcpkg](https://github.com/microsoft/vcpkg)

1. [Setup vcpkg](https://github.com/microsoft/vcpkg#quick-start-windows)
1. Install static fmt packages

    ```powershell
        # https://learn.microsoft.com/en-us/vcpkg/commands/install
        .\vcpkg install fmt:x86-windows-static
        .\vcpkg install fmt:x64-windows-static
    ```

1. Update static fmt packages (already installed)

    ```powershell
        # 1. Navigate to the local vcpkg git directory.
        # 2. Use Git pull to update the local portfile versions.
        git pull

        # https://learn.microsoft.com/en-us/vcpkg/commands/upgrade
        .\vcpkg upgrade fmt:x86-windows-static --no-dry-run
        .\vcpkg upgrade fmt:x64-windows-static --no-dry-run
    ```

1. Find packages in `vcpkg\packages` and copy to `opentelemetry-dotnet-instrumentation\src\OpenTelemetry.AutoInstrumentation.Native\lib`

## spdlog

* Source: <https://github.com/gabime/spdlog>
* Version: 1.12.0

### Setup

Package is acquired via Github release.

1. Download source code (zip) from release
1. Copy `src` and `include` to `opentelemetry-dotnet-instrumentation\src\OpenTelemetry.AutoInstrumentation.Native\lib\spdlog`
1. Resync `src` files and references in `opentelemetry-dotnet-instrumentation\src\OpenTelemetry.AutoInstrumentation.Native\OpenTelemetry.AutoInstrumentation.Native.vcxproj`
