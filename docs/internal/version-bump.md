# Dependency bumping

## Tracked by version

This section describes dependencies that require a periodical version bump.

| Dependency   | Files                      | Bumping    | Notes                                                    |
|--------------|----------------------------|------------|----------------------------------------------------------|
| NuGet        | `*.csproj`, `*.props`      | Dependabot | Test packages might need to stay on a certain version.   |
| GitHub CI    | `./github/workflows/*.yml` | Dependabot | Bumps GitHub step templates                              |
| Docker       | `*.dockerfile`             | Dependabot | Bumps Docker image versions                              |
| Docker       | `docker-compose.yml`       | Manual     | Search for `image:`                                      |
| .NET SDK     | `(CI templates)`           | Manual     | Search for `actions/setup-dotnet` or `dotnetSdkVersion:` |
| GitHub CI OS | `./github/workflows/*.yml` | Manual     | Search for `runs-on:`                                    |
| APK          | `alpine.dockerfile`        | Manual     | Search for `apk add`                                     |

## Tracked by checksum

This section describes dependencies tracked and verified using hardcoded
checksum values.

| Dependency        | Files          | Bumping | Checksum | Notes |
|-------------------|----------------|---------|----------|-------|
| dotnet-install.sh | `*.dockerfile` | Manual  | SHA256   |       |
