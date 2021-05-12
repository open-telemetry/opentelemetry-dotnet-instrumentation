# Releasing

## Pre-Release

### Bumping version

To change the version the following steps have to be executed:
* Update the version number in [TracerVersion.cs](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/tools/Datadog.Core.Tools/TracerVersion.cs).
* Run the [PrepareRelease tool](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/tree/main/build/tools/PrepareRelease): `dotnet run msi versions integrations`.

## Release

To create GitHub release the git tag with correct version number has to be pushed (format is `v\d.\d.\d`, it must match the version prepared in Pre-Release steps). The release will be created automatically by the GitHub Actions workflow with the release notes based on the [CHANGELOG.md](CHANGELOG.md).

The easiest way to create the git tag and trigger the release workflow you should create a tag locally and then push it to the repo. Creating the tag by creating a release in the GitHub UI will not trigger the release workflow. Before creating the tag locally, [ensure that you have a signing key configured and enabled](https://docs.github.com/en/github/authenticating-to-github/telling-git-about-your-signing-key) so that the tag will be marked as `verified`. You can use the following steps to create a tag locally after setting the `tag` (should match the version being released), `commit` (the SHA of the commit we want to tag and create the release with), and `remote` (the repository name configured locally for this repo) variables appropriately.
```
tag=vX.Y.Z
commit=SHA
remote=upstream
git tag -a $(tag) -s -m "Version $(tag)" $(commit)
git push $(remote) $(tag)
```
