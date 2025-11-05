# AGENTS: OpenTelemetry.AutoInstrumentation.Native

## Quick facts
- Purpose: native CLR profiler that bootstraps and manages OpenTelemetry .NET auto-instrumentation.
- Language/toolchain: C++17; clang/cmake on Linux/macOS, MSBuild/CL on Windows. Build orchestration is handled through `dotnet nuke` targets.
- Primary references: `docs/design.md` (architecture) and `docs/developing.md` (build/test/format). Native dependency maintenance lives in `docs/internal/native-dependencies.md`.
- Formatter: `./scripts/format-native.sh` (wraps clang-format from `scripts/download-clang-tools.sh`).
- Tests: `dotnet nuke RunNativeTests` (Windows only today) executes Google Test binaries under `test/OpenTelemetry.AutoInstrumentation.Native.Tests`. Linux/macOS native tests are stubbed out; rely on managed integration tests under `test/test-applications` when working off Unix hosts.

## Code map
- `dllmain.cpp` / `dllmain.h`: COM entry points and class factory registration. `DllMain` creates the profiler instance consumed by the CLR.
- `cor_profiler.cpp` / `.h`: primary `ICorProfilerCallback` implementation. Handles initialization, event mask configuration, environment inspection, and coordinates module/method rewrites.
- `cor_profiler_base.*` and `class_factory.*`: shared COM glue for all profiler instances, plus feature toggles (fail-fast, logging levels, etc.).
- `rejit_handler.*`, `rejit_preprocessor.*`, `rejit_work_offloader.*`, `method_rewriter.*`: manage the ReJIT pipeline that rewrites IL using CallTarget stubs on module load and later on-demand.
- `il_rewriter*.{h,cpp}` and `module_metadata.*`: wrappers around CLR metadata/IL APIs used to edit method bodies safely.
- `stub_generator.cpp`, `calltarget_tokens.cpp`, `tracer_tokens.cpp`: build signature blobs and IL stubs that bridge managed CallTarget instrumentations. These must stay aligned with the managed instrumentation definitions under `src/OpenTelemetry.AutoInstrumentation/Instrumentations`.
- `integration.*`: parses and caches managed assembly signatures; fed into the ReJIT planner when deciding whether a target module should be instrumented.
- `continuous_profiler*.{h,cpp}`, `stats.h`, `thread_span_context.*`: native support for continuous profiling, keeping lightweight spans in sync with managed code.
- `environment_variables*.{h,cpp}`: declares and parses every OTEL_* knob consumed by the native layer. Updates here usually require matching doc changes and test coverage (`SmokeTests.NativeLogsHaveNoSensitiveData` relies on the allow-list in `cor_profiler.cpp`).
- `pal.h`, `util.*`, `string_utils.*`: cross-platform helpers for file system, environment access, conversions, and string handling. Use these instead of raw platform APIs to preserve portability.
- `lib/`: vendored, version-controlled dependencies (`coreclr` headers, `spdlog`, `fmt`). Non-Windows builds also download `re2` at configure time.
- `build/`: cmake-generated artifacts; do not commit manual changes here.

## Execution flow
1. `DllMain` registers `ClassFactory` so the CLR can instantiate `CorProfiler` via COM.
2. `CorProfiler::Initialize` negotiates the highest available `ICorProfilerInfo` interface, validates runtime prerequisites (desktop vs core, version >= 6.0), and reads relevant environment variables. Secrets are filtered before logging.
3. Initialization wires up startup hooks (`StartupHook`), netfx assembly redirects (Windows), and continuous profiler state, then sets event masks for module loads, JIT notifications, and ReJIT callbacks.
4. As assemblies load, `ModuleManager` and `ModuleMetadata` capture signature information. `RejitPreprocessor` checks managed integration metadata to determine which methods require rewriting.
5. `StubGenerator` and token helpers emit IL stubs that call into managed CallTarget methods. `MethodRewriter` replaces target method bodies with the generated IL; instrumentation metadata is stored for future ReJIT cycles.
6. Continuous profiler and interop helpers (`interop.cpp`) expose native functionality to managed code via `NativeMethods` P/Invoke entry points.

## Dependencies and build requirements
- Non-Windows builds require: clang/clang++, gcc (for vendored libs), cmake 3.10–3.19, make, git. `CMakeLists.txt` will fetch and build `re2` and compile `fmt` from the vendored sources under `lib/`.
- Windows builds rely on the Visual Studio toolchain. `dotnet nuke CompileNativeSrc` will invoke `MSBuild` for both x64 and x86 when the platform is x64.
- Cross-platform abstractions live in `pal.h` and `string_utils.*`. Always go through these helpers for filesystem, environment, and string conversions to avoid subtle platform regressions.
- `version.h` and `otel_profiler_constants.h` receive version numbers via build properties (`OTEL_AUTO_VERSION_*`). Do not hardcode semantic versions in new code.

## Common workflows
- **Build the native profiler**: from the repository root, run `dotnet nuke CompileNativeSrc`. This orchestrates either MSBuild or cmake/make depending on your OS and drops artifacts in `bin/tracer-home/<platform>/`.
- **Run native tests**: `dotnet nuke RunNativeTests` (Windows only) executes the Google Test suite. On Linux/macOS, rely on managed integration tests (`dotnet nuke Test`) until native tests gain coverage.
- **Format C++**: install tools with `./scripts/download-clang-tools.sh` once, then run `./scripts/format-native.sh` prior to commits or when addressing review feedback.
- **Refresh vendored deps**: follow `docs/internal/native-dependencies.md`. Changes usually require adjusting `OpenTelemetry.AutoInstrumentation.Native.vcxproj` and keeping `lib/` in sync.
- **Enable IL diagnostics**: set `OTEL_DOTNET_AUTO_DUMP_ILREWRITE_ENABLED=true` to log original vs rewritten IL in debug builds when investigating instrumentation issues.

## Development tips
- Keep logging lightweight and avoid leaking secrets. The profiler sanitizes environment variables; maintain that list when adding new variables.
- Use `trace::WSTRING` / `ToWSTRING` helpers for text. Windows builds rely on UTF-16, whereas Unix builds use UTF-16 literals via `char16_t`; mixing raw `std::string` with CLR APIs often fails.
- All profiler-facing APIs execute on CLR threads. Minimize allocations in hot callbacks (e.g., `JITCompilationStarted`) and leverage `Stats` sampling when adding metrics.
- ReJIT operations are asynchronous. Ensure new code handles re-entrancy and module unloads gracefully—most utilities expect you to check `ModuleInfo::is_valid()` and guard against null metadata.
- When touching configuration parsing, synchronize updates across native parser (`environment_variables_util.cpp`), managed bootstrapper, documentation (`docs/config.md`), and tests.
- Continuous profiler integrations route through `interop.cpp`. Any new native/managed bridge requires corresponding P/Invoke definitions in the managed project.

## Troubleshooting
- `dotnet nuke CompileNativeSrc` failures on Linux/macOS often stem from unsupported cmake versions or missing clang. Inspect `src/OpenTelemetry.AutoInstrumentation.Native/build/CMakeFiles/CMakeOutput.log` for hints.
- If the profiler fails to attach at runtime, check native logs under `%ProgramData%\OpenTelemetry .NET AutoInstrumentation\logs\` (Windows) or `/var/log/opentelemetry/dotnet/`. `cor_profiler.cpp`'s `FailProfiler` macro respects `OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED` for aggressive error handling.
- ReJIT-related crashes frequently trace back to stale metadata caches. Verify `RejitHandler::RemoveModule` is invoked during module unloads when introducing new caches or static state.
- Continuous profiler issues often manifest as missing native context frames. Compare behaviour against the sample exporter in `test/test-applications/integrations/TestApplication.ContinuousProfiler`.

## Useful references
- `docs/design.md` (sections “Architecture” and “Bootstrapping”) for the end-to-end flow across native and managed layers.
- `docs/developing.md` for build/test commands, documentation linting, and formatter setup.
- `docs/internal/native-dependencies.md` for updating vendored libraries (`fmt`, `spdlog`, `coreclr` headers).
- Managed instrumentation lives under `src/OpenTelemetry.AutoInstrumentation`. Changes to native IL stubs usually require mirrored updates here.
- Test entry points: `test/OpenTelemetry.AutoInstrumentation.Native.Tests` (Google Test) and managed integration suites under `test/test-applications`.
