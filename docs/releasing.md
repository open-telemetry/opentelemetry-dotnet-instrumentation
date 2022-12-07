# Release Process

1. Update the version in the following files:

   - [`OpenTelemetry.DotNet.Auto.psm1`](../OpenTelemetry.DotNet.Auto.psm1)
   - [`otel-dotnet-auto-install.sh`](../otel-dotnet-auto-install.sh)
   - [`docs/README.md`](./README.md)
   - [`nuget/OpenTelemetry.AutoInstrumentation.nuspec`](../nuget/OpenTelemetry.AutoInstrumentation.nuspec)
   - [`src/OpenTelemetry.AutoInstrumentation/Constants.cs`](../src/OpenTelemetry.AutoInstrumentation/Constants.cs)
   - [`src/OpenTelemetry.AutoInstrumentation/OpenTelemetry.AutoInstrumentation.csproj`](../src/OpenTelemetry.AutoInstrumentation/OpenTelemetry.AutoInstrumentation.csproj)
   - [`src/OpenTelemetry.AutoInstrumentation.Loader/OpenTelemetry.AutoInstrumentation.Loader.csproj`](../src/OpenTelemetry.AutoInstrumentation.Loader/OpenTelemetry.AutoInstrumentation.Loader.csproj)
   - [`src/OpenTelemetry.AutoInstrumentation.Native/CMakeLists.txt`](../src/OpenTelemetry.AutoInstrumentation.Native/CMakeLists.txt)
   - [`src/OpenTelemetry.AutoInstrumentation.Native/Resource.rc`](../src/OpenTelemetry.AutoInstrumentation.Native/Resource.rc)
   - [`src/OpenTelemetry.AutoInstrumentation.Native/otel_profiler_constants.h`](../src/OpenTelemetry.AutoInstrumentation.Native/otel_profiler_constants.h)
   - [`src/OpenTelemetry.AutoInstrumentation.Native/version.h`](../src/OpenTelemetry.AutoInstrumentation.Native/version.h)

1. Update the [CHANGELOG.md](../CHANGELOG.md) with the new release.

1. Stable release only! Update `PublicAPI.Shipped.txt` based on corresponding `PublicAPI.Unshipped.txt`.

1. Create a pull request with `release PR` label on GitHub
   with the changes described in the changelog.

1. Run the integration tests with Linux containers on Windows and macOS
   (not covered by CI):

   ```bash
   nuke Workflow
   ```

1. Once the pull request has been merged, create a signed tag for the merged commit.
   You can do this using the following Bash snippet:

   ```bash
   TAG='v{new-version-here}'
   COMMIT='{commit-sha-here}'
   git tag -s -m $TAG $TAG $COMMIT
   git push upstream $TAG
   ```

   After you've pushed the git tag, a `release` GitHub workflow starts.

1. Publish a release in GitHub:

   - Use the [CHANGELOG.md](../CHANGELOG.md) content in the description.
   - Add the artifacts from [the `release` GitHub workflow](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/actions/workflows/release.yml).

   After you've publish the release, a `release-publish` GitHub workflow starts.

1. Check the status of [the `release-publish` GitHub workflow](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/actions/workflows/release-publish.yml).

1. For a non-RC and non-beta release, update the version in:

   - [`examples/Dockerfile`](../examples/Dockerfile)
   - [OpenTelemetry Operator](https://github.com/open-telemetry/opentelemetry-operator/blob/main/autoinstrumentation/dotnet/version.txt)
