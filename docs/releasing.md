# Release Process

1. Update the version in the following files:

   - [`download.sh`](../download.sh)
   - [`docs/README.md`](./README.md)
   - [`src/OpenTelemetry.AutoInstrumentation.Loader/OpenTelemetry.AutoInstrumentation.Loader.csproj`](../src/OpenTelemetry.AutoInstrumentation.Loader/OpenTelemetry.AutoInstrumentation.Loader.csproj)
   - [`src/OpenTelemetry.AutoInstrumentation.Native/CMakeLists.txt`](../src/OpenTelemetry.AutoInstrumentation.Native/CMakeLists.txt)
   - [`src/OpenTelemetry.AutoInstrumentation.Native/Resource.rc`](../src/OpenTelemetry.AutoInstrumentation.Native/Resource.rc)
   - [`src/OpenTelemetry.AutoInstrumentation.Native/otel_profiler_constants.h`](../src/OpenTelemetry.AutoInstrumentation.Native/otel_profiler_constants.h)
   - [`src/OpenTelemetry.AutoInstrumentation.Native/version.h`](../src/OpenTelemetry.AutoInstrumentation.Native/version.h)
   - [`src/OpenTelemetry.AutoInstrumentation/OpenTelemetry.AutoInstrumentation.csproj`](../src/OpenTelemetry.AutoInstrumentation/OpenTelemetry.AutoInstrumentation.csproj)
   - ['src/OpenTelemetry.AutoInstrumentation/Constants.cs'](../src/OpenTelemetry.AutoInstrumentation/Constants.cs)
   - ['nuget/OpenTelemetry.AutoInstrumentation.nuspec'](../nuget/OpenTelemetry.AutoInstrumentation.nuspec)

1. Update the [CHANGELOG.md](../CHANGELOG.md) with the new release.

1. Stable release only! Update `PublicAPI.Shipped.txt` based on corresponding `PublicAPI.Unshipped.txt`.

1. Create a pull request on GitHub with the changes described in the changelog.

1. Run the integration tests with Linux containers on Windows and macOS
   (not covered by CI):

   ```bash
   nuke Workflow --containers linux
   ```

1. Test the described [examples](../examples/README.md).

1. Once the pull request has been merged, create a signed tag for the merged commit.
   You can do this using the following Bash snippet:

   ```bash
   TAG='v{new-version-here}'
   COMMIT='{commit-sha-here}'
   git tag -s -m $TAG $TAG $COMMIT
   git push {remote-to-the-main-repo} $TAG
   ```

   After you've pushed the git tag, a `release` GitHub workflow starts.

1. Publish a release in GitHub:

   - Use the [CHANGELOG.md](../CHANGELOG.md) content in the description.
   - Add the artifacts from [the `release` GitHub workflow](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/actions/workflows/release.yml).

1. Update version in `install-script` job in [`.github/workflows/ci.yml`](../.github/workflows/ci.yml).

1. Update version under [OpenTelemetry Operator](https://github.com/open-telemetry/opentelemetry-operator/blob/main/autoinstrumentation/dotnet/version.txt).
