# How to perform pull from upstream repo

1. Fetch upstream and origin, be sure to update main branch and create a new branch, let's say pull-upstream-changes
2. Find the range of commits from upstream:
    * Look at previous catchup PR comment and record the sha of the last commit on upstream, let's call it <fst_sha>
    * Look at the last commit on upstream, eg.: git log --oneline upstream/master, let's call it <last_sha>
3. If needed to squash to pass CLA check:
    * Add an empty commit and save its sha (squash_sha): `git commit --allow-empty -m "Catchup to upstream"`
4. `git cherry-pick fst_sha..last_sha`
    * Resolve each conflict and `git cherry-pick --continue` every time that one is resolved
5. Build and fix any integration issues:
    * New usages of env vars, reg ex: ^[^#].*[^A-Z]DD_
    * Old profiler ID: 846F5F1C-F9AE-4B07-969E-05C26BC060D8 (happens in launch.settings for new apps).
    * Run unit tests, commit any needed fixes, repeat until passing unit tests
    * Update version in [TracerVersion.cs](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/tools/Datadog.Core.Tools/TracerVersion.cs) if the upstream bumped it.
    * Update versions and integrations json by running: ` cd \build\tools\PrepareRelease && dotnet run -- versions integrations` (remember to revert wcf and other windows-only frameworks if you are using different platform)
    * Run each of the following commands and commit any needed fixes, until it passes:
       - `docker-compose run --rm build`
       - `docker-compose run --rm Profiler`
       - `docker-compose run --rm IntegrationTests`
       - `docker-compose run --rm package`

6. If squashing cherry-pick from upstream to pass CLA check:
    * `git rebase -i <squash_sha>^`
    * Select top one as "pick" all coming from upstream as "squash" and let the ones that you made to fix build and test as "pick" so it is easier to review them separately.
