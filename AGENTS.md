# AGENTS.md

This file provides guidance to coding agents when working with code in this repository.

## What this repository is

OpenTelemetry .NET Automatic Instrumentation: adds OpenTelemetry to .NET and
.NET Framework applications without source code changes, via the .NET host
startup hook and/or a native CLR Profiler that rewrites IL at runtime.

## Build and test

Builds are driven by [Nuke](https://github.com/nuke-build/nuke). `./build.cmd`
(Windows) / `./build.sh` (Linux, macOS) bootstrap `dotnet nuke`; run
`dotnet tool restore` first if using `dotnet nuke` directly. The default target
is `BuildTracer`; main artifact is `bin/tracer-home`.

```cmd
dotnet nuke --help                 :: list all targets and parameters
./build.cmd                        :: BuildTracer - native + managed src -> bin/tracer-home
./build.cmd BuildNativeWorkflow    :: native only
./build.cmd Workflow               :: full build + all tests (what CI approximates)
./build.cmd NativeTests            :: build and run C++ unit tests
./build.cmd ManagedTests           :: build and run managed unit + integration tests
./build.cmd TestWorkflow           :: NativeTests + ManagedTests, assumes BuildTracer already ran
./build.cmd BuildNuGetPackages     :: needs CI artifacts, see docs/developing.md
```

Useful parameters (apply to the test targets):

- `--test-project <substring>` — select test projects by name substring.
- `--test-name <substring>` — becomes an xunit `FullyQualifiedName~` filter.
- `--test-target-framework net8.0|net9.0|net10.0|net462|net472|...` — otherwise
  every TFM the project supports is run.
- `--containers linux|windows|windows-only|none` — integration tests are
  tagged `[Trait("Containers", "Linux"|"Windows")]`; use `none` to skip every
  test needing Docker.
- `--test-count N` — repeat runs, for flakiness checks.
- `--configuration Debug`, `--platform x86|x64|ARM64`, `--skip <target>`.

Running one integration test:

```cmd
./build.cmd BuildTracer
./build.cmd --target ManagedTests --test-project IntegrationTests --test-name MongoDBTests --test-target-framework net8.0
```

Integration tests can also be run straight from an IDE / `dotnet test` once
`bin/tracer-home` exists, but then only the latest library version of each
instrumented package is exercised (Nuke runs the full version matrix).

Test logs and profiler logs land in `test-artifacts/`.

### Lint and format

- `dotnet format .\OpenTelemetry.AutoInstrumentation.sln --verify-no-changes` —
  exactly what the `dotnet format` CI check runs. Warnings are errors
  (`TreatWarningsAsErrors`), analysis level is `latest-All`, StyleCop is on
  solution-wide, nullable + implicit usings are enabled.
- `./scripts/format-native.sh` (after `./scripts/download-clang-tools.sh`) for
  C++.
- `nuke InstallDocumentationTools ValidateDocumentation` for markdownlint +
  cspell; `nuke MarkdownLintFix` auto-fixes. Requires Node.js.

### Local telemetry backend

`docker compose -f dev/docker-compose.yaml up` starts a Collector + Jaeger
(<http://localhost:16686/search>, metrics on `:8889`, files in `dev/log`).
`OTEL_DOTNET_AUTO_HOME=bin/tracer-home . ./instrument.sh` exports the profiler
env vars into the current shell, or `./instrument.sh dotnet MyApp.dll` to launch
an instrumented app directly. `examples/playground` is a scratch app for this.

## Architecture

Read `docs/design.md` for the full picture. The moving parts:

- `src/OpenTelemetry.AutoInstrumentation.Native` — C++ CLR Profiler
  (`cor_profiler.cpp`, `il_rewriter*`, `method_rewriter.cpp`, plus the
  CallTarget token helpers). Receives instrumentation definitions from managed
  code, requests ReJIT for target methods, and rewrites their IL. On .NET
  Framework it also injects the Loader at startup and performs assembly
  reference redirection.
- `src/OpenTelemetry.AutoInstrumentation.StartupHook` — .NET-only entry point
  (`StartupHook.Initialize()`), loaded via `DOTNET_STARTUP_HOOKS`.
- `src/OpenTelemetry.AutoInstrumentation.Loader` — creates
  `Loader.Startup`, hooks `AssemblyResolve`/`AssemblyLoadContext` so SDK and
  instrumentation assemblies resolve, then reflectively calls
  `Instrumentation.Initialize` in the managed profiler.
- `src/OpenTelemetry.AutoInstrumentation` — the managed profiler: SDK setup
  (`Instrumentation.cs`, `Configurations/`), CallTarget infrastructure
  (`CallTarget/`), DuckTyping (`DuckTyping/`), bytecode instrumentations
  (`Instrumentations/`), plugin hooks (`Plugins/`), vendored code (`Vendors/`).
- `src/OpenTelemetry.AutoInstrumentation.Assemblies{,.NetFramework}` — pull in
  the OpenTelemetry SDK + instrumentation library packages that get copied into
  the distribution; `...AdditionalDeps` produces the additional-deps files.
- `src/SourceGenerators` — `InstrumentationDefinitionsGenerator` scans
  `[InstrumentMethod]` attributes and emits the `InstrumentationDefinitions`
  partial (`GetDefinitionsArray`) that is handed to the native side as
  `NativeCallTargetDefinition[]`. Generated output is under
  `src/OpenTelemetry.AutoInstrumentation/Generated/<tfm>/`.
- `src/OpenTelemetry.AutoInstrumentation.PluginApi`, `nuget/` — public plugin
  API and the NuGet package layouts (`OpenTelemetry.AutoInstrumentation`,
  `...Runtime.Native`).
- `build/` — the Nuke project (`Build.cs` + `Build.Steps*.cs` per platform,
  `Projects.cs` names all projects, `TargetFramework.cs` the TFM matrix).
- `tools/LibraryVersionsGenerator` — generates the tested-library version
  matrix.

Two instrumentation styles coexist: **source instrumentations** (upstream
`OpenTelemetry.Instrumentation.*` libraries, enabled lazily on `AssemblyLoad`)
and **bytecode instrumentations** (this repo's own, injected by the profiler).

### Adding or changing a bytecode instrumentation

An instrumentation is a static class under
`Instrumentations/<Library>/Integrations/` annotated with one or more
`[InstrumentMethod(assemblyName, typeName, methodName, returnTypeName,
parameterTypeNames, minimumVersion, maximumVersion, integrationName, type)]`
attributes, exposing `OnMethodBegin`/`OnMethodEnd` (or
`OnAsyncMethodEnd`) returning `CallTargetState`/`CallTargetReturn`. Nothing
needs to be registered manually — the source generator picks the attributes up.

Instrumentation code must not reference the instrumented library directly (one
build has to work across many library versions): use `DuckTyping/` interfaces
(see its `README.md`) or reflection to touch library types. Add the
integration name to `Configurations/TracerInstrumentation.cs` /
`MetricInstrumentation.cs` / `LogInstrumentation.cs` as appropriate and document
it in `docs/config.md`.

### Tests

- `test/OpenTelemetry.AutoInstrumentation.Tests`, `...Loader.Tests`,
  `...StartupHook.Tests`, `...BuildTasks.Tests` — managed unit tests.
- `test/OpenTelemetry.AutoInstrumentation.Native.Tests` — C++ (gtest).
- `test/OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests` — run by the
  `RunManagedUnitTests` target through a dedicated harness, not plain
  `dotnet test`.
- `test/IntegrationTests` — the highest-value suite: launches a real
  instrumented app from `test/test-applications/integrations/` against mock
  OTLP collectors (`Helpers/Mock*Collector.cs`, `TestHelper.cs`) and asserts on
  the exported telemetry. One test class per instrumented library; feature
  work without a dedicated app goes into `SmokeTests`. Some assertions use
  Verify snapshots (`*.verified.txt`).
- `test/NuGetPackagesTests` + `test/test-applications/nuget-packages/` — the
  NuGet deployment mode.

Library version coverage strategy (lowest non-vulnerable, one per major, latest,
plus versions with breaking changes) lives in
`tools/LibraryVersionsGenerator/PackageVersionDefinitions.cs`; edit it, run the
generator (or `nuke GenerateLibraryVersionFiles`), and commit the generated
`LibraryVersions.g.cs` files. Latest supported versions are pinned in
`test/Directory.Packages.props`.

To check whether a test is flaky, trigger the `verify-test.yml` workflow
manually rather than looping locally.

## Conventions

- Central package management: versions live in `Directory.Packages.props` files
  (root, `src/`, `test/`, `tools/`, `src/OpenTelemetry.AutoInstrumentation.Assemblies/`);
  don't put versions on `PackageReference`.
- Version numbers come from git tags via MinVer (`v` prefix) — don't hand-edit
  version properties.
- Update `CHANGELOG.md` under `## [Unreleased]` for user-visible changes, and
  `docs/config.md` for anything configurable. PRs are expected to be small and
  single-concern.
- `Vendors/` files are copied verbatim from upstream projects and must not be
  hand-edited; see `docs/internal/vendored-code.md` for the mechanical changes
  applied when vendoring (namespace prefixing, making the contract internal).
- `*.g.cs` files and `src/OpenTelemetry.AutoInstrumentation/Generated/` are
  generated — change the generator or its input instead.
- Instrumentation must never crash the host application: runtime errors are
  logged (`AutoInstrumentationEventSource`, `Logging/`) and swallowed;
  only invalid configuration at init may fail (see `FailFastSettings`).
- Clean with `git clean -fXd` (the Nuke `Clean` target covers the common cases).

## What maintainers actually push back on

Synthesized from maintainer review comments on PRs, May–August 2026. These are
the things CI cannot tell you; they are the reasons PRs stall, get converted to
draft, or get closed.

### Disclose AI assistance the way this project requires

- Use an `Assisted-by: <model>` commit trailer (per the
  [OpenTelemetry GenAI policy](https://github.com/open-telemetry/community/blob/main/policies/genai.md)),
  **not** `Co-authored-by:` — even if your tooling defaults to the latter. A PR
  has been held with "without this we cannot proceed" purely over this.
- Write the PR body yourself, following the `Why` / `What` / `Tests` template.
  "Some meaningful PR description" was requested in the same breath as the
  trailer fix. Maintainers may close contributions that read as unedited model
  output, and pasting model output into review threads instead of answering the
  question is explicitly discouraged by that policy.
- Reviewers are already running their own AI passes over PRs and will paste
  concrete findings; treat those like any other review comment and answer with
  reasoning, not with more generated prose.

### One concern per PR, and every line explainable

- Unrelated fixes found along the way get sent to their own PR ("Can we make
  this fix separate from this PR?"), even genuinely good ones like a COM leak
  fix inside a feature branch. Adjacent tooling ideas likewise — a new
  security-lint workflow was asked to be its own PR rather than a rider.
- Incidental churn draws review comments more reliably than anything else:
  changed quoting style, an extra config key, one line removed too many, a dead
  file added to a `vcxproj`. Expect "What is the reason for this change?" and a
  stalled PR. Revert anything you cannot justify in one sentence.
- Improvement ideas raised in review are usually deferred to follow-up issues on
  purpose — "one step at a time". Don't grow the diff in response to them; agree
  on an issue instead.
- Preserve existing behavior when refactoring, and say so explicitly. Reviewers
  do diff the semantics: a `PluginManager` rewrite was caught returning after the
  first plugin where the original looped over all of them.

### Telemetry attributes need a *released* semantic convention

- A merged semantic-conventions PR is not enough — maintainers block until the
  convention appears in a stable semconv release, and will file
  `CHANGES_REQUESTED` "to avoid unintentional merge". If the convention doesn't
  exist yet, the ask is to go and fix semconv first.
- Check the exact attribute name against the semconv version in use rather than
  from memory (`service.namespace` vs `service.namespace.name` was corrected in
  review), and note in the PR when a convention is still
  Experimental/Development.

### Dependency versions are a deliberate policy, not "latest"

- The SIG-agreed strategy (issue #4874) for what we distribute is the *lowest
  non-vulnerable* version matching the target framework major, with .NET
  Framework matching what the latest .NET uses. Bumping
  `src/OpenTelemetry.AutoInstrumentation.Assemblies/Directory.Packages.props`
  toward latest gets rejected as "against the discussed dependency management
  strategy".
- A version belongs in the highest `Directory.Packages.props` that needs it —
  putting a package needed by both `src/` and `test/` only in
  `test/Directory.Packages.props` draws a review comment.
- Before proposing a library upgrade, check the package's dependency tab: the
  usual blockers are loss of `net462` support, the legacy Ubuntu native build
  container's toolchain, and non-permissive licenses on newer majors. Many bot
  bumps are closed for exactly these reasons, so don't "helpfully" re-apply one.
- When an upstream bug blocks us, maintainers link the upstream issue and wait
  rather than working around it locally.
- New library-version support must be validated on .NET Framework too, not just
  .NET; support quietly added for .NET only has had to be disabled afterwards.

### Behavior and configuration semantics go to the SIG

- Anything touching precedence between configuration sources, per-AppDomain vs
  process-level scope, resource-attribute merging, or whether a change is
  minor-or-major gets escalated to a SIG meeting and decided there. Present the
  options and the compatibility implications; do not pick one unilaterally and
  ship it.
- Avoid leaving a mixed semantic model (some keys AppDomain-local, others
  promoted to process environment variables) — that inconsistency, not the
  change itself, is what drew objection.
- Environment-variable defaults and file-based (YAML) config defaults are not
  always the same. Documentation claims about a default are checked against both
  (`docs/file-based-configuration.md` corrections: "Not true for file based
  configuration").
- Public plugin/extension surface is treated as a long-lived contract tied to
  the distro release cadence; propose API shape in an issue or the SIG before
  writing it.

### Tests: coverage is expected, runtime is a budget

- Feature PRs arrive with integration tests or get asked for them ("I think that
  this PR is missing some integration tests"), including for native/profiler
  work where existing test apps only incidentally cover the path.
- The converse is also enforced: a technically-correct test that needed a ~30
  second wait was asked to be deleted because the suite is already long. Prefer
  extending an existing test application over adding a new one, and don't add
  assertions that need sleeps.
- Name test classes, test applications, and helpers after the scenario they
  cover so the intent is obvious from the suite listing.
- Tests that document deliberately odd behavior are welcome when the comment
  explains why the behavior is by design.

### Native (C++) code gets a stricter read

- Comment the cross-platform reasoning: character widths, `int`/`long` sizes and
  pointer arithmetic differences between Windows and Linux are the reviewers'
  stated concern; use the `WStr` macro for character literals instead of raw
  prefixes.
- Manage COM lifetimes with `ComPtr` rather than raw interface pointers.
- No unused declarations, no files added to the project that nothing builds, and
  file names must describe what the file actually contains.
- Changes to profiler runtime behavior — thread suspension, stack walking, any
  loader call made while a sampled thread is suspended — attract requests for an
  opt-out switch and for symmetry with existing walk paths. Assume conservative
  reviewers here and explain the failure modes you considered.
- `src/OpenTelemetry.AutoInstrumentation.Native/lib/` is vendored code (see
  `docs/internal/native-dependencies.md`); exclude it from scans and don't
  restyle it.

### Triage CI before asking for a review

- Merge `main` into the branch first. "Most/all of your failings is related to
  already fixed stuff" is a routine comment.
- Recognize the infrastructure failure modes that are not your change: Azurite
  API-version mismatches, stale container digests, arm container timeouts, and
  the timing-sensitive continuous-profiler/collector-expectation tests. There is
  no automatic test-retry layer, so a single red leg is not proof of a
  regression — confirm with `verify-test.yml` before "fixing" it.
