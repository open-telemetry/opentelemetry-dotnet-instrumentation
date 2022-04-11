# Release Process

1. Update the version in the following files:

   - `*.cs` - TODO: extract as a constant to reduce the number of occurances
   - `otel_profiler_constants.h`
   - `version.h`
   - `OpenTelemetry.AutoInstrumentation.csproj`
   - `OpenTelemetry.AutoInstrumentation.Core.csproj`
   - `OpenTelemetry.AutoInstrumentation.Loader.csproj`
   - `CMakeLists.txt`
   - `Resource.rc`

2. Update the [CHANGELOG.md](../CHANGELOG.md) with the new release.

3. Create a Pull Request on GitHub with the changes above.

4. Once the Pull Request merged
   it is time to create a signed tag for the merged commit.

   You can do this using the following Bash snippet.

   ```bash
   TAG='v{new-version-here}'
   COMMIT='{commit-sha-here}'
   git tag -s -m $TAG $TAG $COMMIT
   git push {remote-to-the-main-repo} $TAG
   ```

   After you push the Git tag, a `release` GitHub workflow should start.

5. Publish a GitHub release:

   - Use the [CHANGELOG.md](../CHANGELOG.md) content in the description.
   - Add the artifacts from [the `release` GitHub workflow](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/actions/workflows/release.yml).
