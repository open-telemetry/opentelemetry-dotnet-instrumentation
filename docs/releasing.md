# Release Process

1. Update stability status in [`config.md`](config.md) if needed.

1. Update documentation links to refer to a tag instead of `main` branch.

1. Update the version in the following files:

   - [`docs/README.md`](./README.md)

1. Update the [CHANGELOG.md](../CHANGELOG.md) with the new release.
   Remove empty sections for the version being released.

1. Stable release only! Update `PublicAPI.Shipped.txt` based on corresponding `PublicAPI.Unshipped.txt`.

1. Create a pull request with `release PR` label on GitHub
   with the changes described in the changelog.

1. Add tests section in pull request description displaying current status of testing:

     ```markdown
    ## Tests

    - [ ] CI
    - [ ] MacOS with Linux Containers
    - [ ] Windows with Linux Containers
     ```

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
   This will create draft release with uploaded artifacts.

1. Publish a release in GitHub:

   - Use draft created by `release` GitHub workflow.
   - Use the [CHANGELOG.md](../CHANGELOG.md) content in the description.

   After you've publish the release, a `release-publish` GitHub workflow starts.

1. Check the status of [the `release-publish` GitHub workflow](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/actions/workflows/release-publish.yml).

1. If the `release-publish` GitHub workflow succeeds, publish the NuGet packages:
    1. Unzip `opentelemetry-dotnet-instrumentation-nuget-packages.zip` to a local
    folder.
    1. Upload and publish the packages (`.nupkg`)
       and corresponding symbol packages (`.snupkg`) to nuget.org.

1. For a stable release, update the version in:

   - [`examples/demo/Dockerfile`](../examples/demo/Dockerfile)
   - [`test/test-applications/nuget-packages/TestApplication.NugetSample/TestApplication.NugetSample.csproj`](../test/test-applications/nuget-packages/TestApplication.NugetSample/TestApplication.NugetSample.csproj)
   - [OpenTelemetry Demo](https://github.com/open-telemetry/opentelemetry-demo/blob/main/src/accounting/Accounting.csproj#L20)

1. For a stable release, update documentation under [opentelemetry.io](https://github.com/open-telemetry/opentelemetry.io/tree/main/content/en/docs/zero-code/dotnet).
