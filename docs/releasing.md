# Release Process

1. Update the version in the following files:

   - `TracerConstants.cs`
   - `otel_profiler_constants.h`
   - `version.h`
   - `OpenTelemetry.AutoInstrumentation.csproj`
   - `OpenTelemetry.AutoInstrumentation.Core.csproj`
   - `OpenTelemetry.AutoInstrumentation.Loader.csproj`
   - `CMakeLists.txt`
   - `Resource.rc`

2. Update the [CHANGELOG.md](../CHANGELOG.md) with the new release.

3. Create a pull request on GitHub with the changes described in the changelog.

4. Once the pull request has been merged, create a signed tag for the merged commit.
   You can do this using the following Bash snippet:

   ```bash
   TAG='v{new-version-here}'
   COMMIT='{commit-sha-here}'
   git tag -s -m $TAG $TAG $COMMIT
   git push {remote-to-the-main-repo} $TAG
   ```

   After you've pushed the git tag, a `release` GitHub workflow starts.

5. Publish a release in GitHub:

   - Use the [CHANGELOG.md](../CHANGELOG.md) content in the description.
   - Add the artifacts from [the `release` GitHub workflow](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/actions/workflows/release.yml).
