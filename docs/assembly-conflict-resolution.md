# Assembly Conflict Resolution

OpenTelemetry .NET Automatic Instrumentation ships its own dependencies
(for example, `OpenTelemetry.dll`, `System.Diagnostics.DiagnosticSource.dll`).
When an instrumented application has the same dependencies at different
versions, a conflict can arise. This document explains how the
instrumentation resolves such conflicts, what the runtime mechanics behind
this are, and what limitations to be aware of.

## How .NET resolves assemblies

Understanding the runtime's assembly-loading pipeline is essential to
understanding why the instrumentation does what it does.

### .NET

On .NET (Core), the assembly-loading subsystem is built around
`AssemblyLoadContext` (ALC). There are two important concepts:

| Concept | Description |
| --- | --- |
| **Default ALC** | The main context that the runtime creates to load application. All application assemblies listed in the Trusted Platform Assemblies (TPA) list are loaded here automatically. |
| **Custom ALC** | A user-created context that can load assemblies independently, providing isolation from the Default ALC. |

#### Assembly binding

Assembly binding happens **once per `AssemblyRef`**: once a reference is
bound to a concrete assembly, the result is cached and no resolution
callbacks fire for that reference again.

#### Resolution order

When code triggers an assembly reference, the runtime first checks
whether the assembly is **already loaded** in the ALC of the assembly
that has the `AssemblyRef` being resolved, at the requested version or
higher. If a match is found, that instance is used immediately and no
further steps run. If an assembly is missing or a same-named assembly
is loaded but at a lower version, the match fails and resolution
continues.

Next, the runtime invokes the current ALC's **`Load()` method**. For the
Default ALC this performs a TPA list lookup. For a custom ALC this calls
the user-provided override. If `Load()` returns an assembly, resolution
stops.

If the current ALC is a custom context and `Load()` returned `null`, the
runtime **falls back to the Default ALC** and checks the TPA list.

If the assembly is still not found, the following events fire in order:

1. **`AssemblyLoadContext.Default.Resolving`**
2. **Custom ALC `Resolving`** (skipped when the current context is the
   Default ALC)
3. **`AppDomain.CurrentDomain.AssemblyResolve`**

This order is important for the assembly conflict resolution
strategies: the custom ALC's `Load()` method provides the earliest
opportunity to supply an assembly when loading to a custom context,
while `Default.Resolving` is the earliest event-based callback for
the Default ALC. Note that
`AppDomain.CurrentDomain.AssemblyResolve` also receives a built-in
handler that automatically resolves co-located dependencies from the
same directory — so subscribing to it may not always be reliable.

#### Key property: type isolation

The same assembly loaded into two different ALCs produces **distinct
types**, with key practical consequences:

- **Shared-state drift**: Static fields are per-instance. Data shared
  through static fields (for example, `System.Diagnostics.Activity.Current`)
  must come from a single assembly instance — otherwise different parts
  of the application will see different state.
- **Cast failures**: Types loaded from different ALC instances of the
  same assembly cannot be cast to each other, even if they have
  identical signatures.

#### Loading restrictions

- If an ALC already contains a same-named assembly at a lower version,
  attempting to load a higher version into the same ALC will fail.
- For the Default ALC, if the TPA list contains a lower version of an
  assembly, loading a higher version will fail. Conversely, if the TPA
  list contains a higher version and a lower version is requested, the
  TPA's higher version is silently used instead.

### .NET Framework

On .NET Framework there is no `AssemblyLoadContext` API. Assembly
loading is scoped to the `AppDomain`. While assemblies within a single
`AppDomain` share the same logical load context, there is no explicit
API to manage or observe separate contexts as in .NET. The difference
can only be observed through the behavior of different `Load` methods.
When the runtime cannot resolve an assembly the
`AppDomain.CurrentDomain.AssemblyResolve` event fires, and handlers
can supply the assembly.

## The problem

The instrumentation may depend on a **higher version** of a shared library
than the version the application was built against. For example:

- The application references `System.Diagnostics.DiagnosticSource` 8.0.0
- The instrumentation requires `System.Diagnostics.DiagnosticSource` 10.0.0

If the lower version loads first, the instrumentation may not work
correctly because it relies on APIs that only exist in the higher
version. Conversely, if the instrumentation's higher version loads, it is
usually backward-compatible and both the application and the
instrumentation work correctly.

The goal is to **ensure the highest version wins** while keeping both
the application and the instrumentation using the **same assembly
instance** to avoid shared-state drift.

## Resolution strategies by deployment mode

### 1. NuGet package deployment (.NET and .NET Framework)

When the instrumentation is added as a NuGet package, the .NET SDK
resolves assembly versions at **build time**. The standard NuGet
version-unification rules guarantee that the highest referenced version
of each dependency is selected for the output. No special runtime
conflict resolution is needed in this case.

> **Note:** If the application directly references a lower version of a
> shared dependency than the instrumentation requires, the application's
> direct reference takes precedence. This will generate a build-time
> warning and may cause runtime failures if the instrumentation relies
> on APIs not present in the lower version.
>
> **Recommendation:** Ensure that your application's dependencies comply
> with the instrumentation's minimum version requirements for guaranteed
> compatibility. Upgrade any conflicting direct references to at least
> the versions required by the instrumentation.

#### Disabling assembly redirection for NuGet deployments

For NuGet package deployments, `OTEL_DOTNET_AUTO_REDIRECT_ENABLED`
must be set to `false`. If enabled, the instrumentation will attempt to
load redirected assembly versions that may not exist in the application's
output, causing the application to crash.

### 2. Standalone: Native profiler deployment (.NET and .NET Framework)

When deployed via the native profiler, the instrumentation does not
participate in the build. It is injected into the application at runtime.

#### IL rewriting of assembly references

As each module loads, the native profiler inspects every assembly
reference (`AssemblyRef`) in the module's metadata. If the
instrumentation ships a higher version of that assembly, the profiler
rewrites the reference in-place to point to the higher version. This is
controlled by a version map compiled into the native profiler (see
[`assembly_redirection_net.h`](../src/OpenTelemetry.AutoInstrumentation.Native/assembly_redirection_net.h)
and
[`assembly_redirection_netfx.h`](../src/OpenTelemetry.AutoInstrumentation.Native/assembly_redirection_netfx.h)).

After rewriting, the runtime proceeds to resolve the rewritten
reference. Because the version now matches what the instrumentation
ships, the resolution follows the
[runtime resolution pipeline](#resolution-order).

The instrumentation ships the latest versions of its dependencies
compatible with each target framework (for example, for `net8.0` the
latest 8.x versions, for `net9.0` the latest 9.x versions). Exception:
`System.Diagnostics.DiagnosticSource` always ships the latest version
across all target frameworks.

#### Managed assembly resolver (.NET)

On .NET, the instrumentation subscribes to
`AssemblyLoadContext.Default.Resolving` — the earliest event-based
callback in the [resolution order](#resolution-order) — to reliably
supply an assembly before any other handler runs. Because assembly
references have already been rewritten by the native profiler, this
event normally fires with the instrumentation's exact version. In the
standard case, the resolver knows exactly what to do — decide which
context to load the assembly into:

| Situation | Why it fires | Where we load the assembly |
| --- | --- | --- |
| Assembly is **not** in the TPA list (for example, `OpenTelemetry.dll`) | The runtime has no default location for it | **Default ALC** — no conflict risk |
| Assembly **is** in the TPA list but with a **lower** version | The profiler rewrote the reference to a higher version that the TPA cannot satisfy | **Custom ALC** — loading into the Default ALC would fail because the TPA already provides a lower version |

When a TPA-conflicting assembly is loaded into a custom ALC it is
isolated from the Default ALC version. Note that if the TPA already
has the same or a higher version, the runtime satisfies the reference
automatically and the event never fires — no action is needed.

However, other situations may also trigger the Resolving event for
an assembly the instrumentation ships (e.g., programmatic
Assembly.Load with an explicit version). To avoid accidentally
satisfying a request that is not ours or one we cannot fulfill, the
resolver validates versions before loading: it only proceeds if the
instrumentation's assembly version is **equal to or higher than**
the requested version. If the requested version is higher than what
the instrumentation ships, the resolver skips the request and lets
other handlers or the runtime deal with it.

#### Managed assembly resolver (.NET Framework)

On .NET Framework, the instrumentation subscribes to
`AppDomain.CurrentDomain.AssemblyResolve` and loads the required
assemblies from the instrumentation's home directory using
`Assembly.LoadFrom`. The instrumentation ships the latest supported
versions of its dependencies and the profiler's IL rewriting forces the
application to use these versions.

#### .NET Framework-specific complexities

.NET Framework has additional assembly resolution behaviors that can
affect the instrumentation:

**Multiple AppDomains:** If the application creates multiple
`AppDomain` instances, each `AppDomain` has its own assembly resolution
context. The instrumentation's `AssemblyResolve` handler is registered
per-`AppDomain`, so assemblies must be resolved independently in each
one.

**Global Assembly Cache (GAC) override:** Assemblies in the GAC take
precedence over other resolution mechanisms. If a conflicting version
exists in the GAC, it will be loaded instead of the instrumentation's
version, regardless of IL rewriting or `AssemblyResolve` handlers.

For automatic redirection to work, there are two specific scenarios
that require the instrumentation's .NET Framework assemblies (in the
`netfx` folder) to also be installed into the GAC:

1. Bytecode instrumentation of assemblies loaded as domain-neutral
2. Assembly redirection for strong-named applications that also ship
   different versions of assemblies included in the `netfx` folder

**`assemblyBinding` redirect configuration:** Application or machine
configuration files (`app.config`, `web.config`, `machine.config`) can
contain `<assemblyBinding>` redirects. These redirects take precedence
over the instrumentation's runtime handlers. If the application has
existing binding redirects that conflict with the instrumentation's
requirements, automatic redirection may fail. Check that no existing
binding redirects prevent redirection to the versions listed in
[`assembly_redirection_netfx.h`](../src/OpenTelemetry.AutoInstrumentation.Native/assembly_redirection_netfx.h).

### 3. Standalone: StartupHook-only deployment (.NET only)

When the native profiler is not attached, the instrumentation relies
solely on the .NET
[startup hook](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md).
Without the native profiler there is no IL rewriting, and without a
NuGet package there is no build-time version resolution.

In this mode the runtime loads the customer application and its
dependencies into the Default ALC automatically. Once they are there,
the instrumentation cannot replace them with higher versions.

**Solution — application isolation:** The startup hook creates an
isolated ALC and:

1. Loads the customer's entry assembly into the isolated ALC.
2. Adjusts the runtime environment so that framework APIs (such as
   `Assembly.Load` and `Assembly.GetEntryAssembly`) resolve correctly
   within the isolated context.
3. Initializes the instrumentation inside the same isolated ALC.
4. For every dependency requested by either the application or the
   instrumentation, the isolated ALC's `Load` method compares the
   version available in the TPA list against the version shipped by
   the instrumentation and **picks the higher one**. Before loading,
   it also validates that the selected version is **equal to or higher
   than** the requested version. If the best available version is still
   lower than what was requested, the instrumentation skips the request
   rather than loading an incompatible version.
5. Invokes the customer's `Main` entry point via reflection, then calls
   `Environment.Exit` to prevent the runtime from re-executing the
   application in the Default ALC.

If setup fails, the isolation is reverted and the runtime falls back to
executing the application normally (unless fail-fast mode is enabled via
`OTEL_DOTNET_AUTO_FAIL_FAST`).

## Known limitations

The resolution strategies described above cover the majority of cases,
but there are scenarios where they cannot fully control assembly loading.

### Explicit loading into the Default ALC

If application code explicitly calls
`AssemblyLoadContext.Default.LoadFromAssemblyPath` (or
`Assembly.LoadFrom`, which loads into the Default ALC) for an assembly
that the instrumentation also depends on, the instrumentation cannot
prevent or override that load.

- **Native profiler deployment:** This is less of a problem in practice.
  Because the profiler has already rewritten all `AssemblyRef` entries to
  point to the instrumentation's versions, the explicitly loaded assembly
  will typically not be referenced by any rewritten code. However, if
  reflection code is called after the load from the explicitly loaded
  assembly, issues may arise as there are now multiple types from the
  same-named assembly. In most cases there is no shared-state drift, but
  the unused assembly remains loaded in memory.
- **StartupHook-only deployment:** This is more impactful. The
  application and instrumentation run inside the isolated ALC, but an
  explicit load into the Default ALC creates a second copy of the
  assembly. Code that crosses the ALC boundary may encounter type
  mismatches or shared-state drift (for example,
  `System.Diagnostics.Activity.Current` seen from two different
  `DiagnosticSource` instances).

### StartupHook-only: the customer application is loaded twice

In isolated mode the startup hook loads the customer's entry assembly
into the isolated ALC. However, the .NET runtime has already loaded the
same entry assembly into the Default ALC before the startup hook runs.
This means the entry assembly exists in both contexts. The startup hook
calls `Environment.Exit` after the isolated execution completes to
prevent the runtime from executing the Default-ALC copy, but the
duplicate load itself is unavoidable.

### StartupHook-only: inevitable assembly leakage to the Default ALC

The startup hook runs after the .NET host has already initialized. Any
assemblies loaded before the hook — including the startup hook assembly
itself, its direct dependencies, and any other `DOTNET_STARTUP_HOOKS`
that appear earlier in the list — are already in the Default ALC.
Additionally, assemblies that *must* remain in the Default ALC (such as
`System.Private.CoreLib`) always load there. These assemblies cannot be
controlled by the isolated context.

Note: If the instrumentation is the only startup hook, this is not
typically a problem — the startup hook assembly and its own dependencies
are known and have been validated to be safe in the isolated context.
However, if there are additional startup hooks, conflicts may arise.

### Custom `AssemblyResolve` / `Resolving` handlers in application code

If the application subscribes to `AppDomain.CurrentDomain.AssemblyResolve`
or `AssemblyLoadContext.Default.Resolving` and loads a **different
version** of an assembly that the instrumentation has already resolved,
the behavior is unpredictable:

- If the instrumentation's handler resolved first (assembly binding is
  cached), the application's handler will never fire for that reference.
- If the application's handler runs first (for example, it was
  subscribed before the instrumentation), it may load a version that the
  instrumentation is incompatible with.

This scenario is relatively rare because few applications implement
custom assembly resolution, but it can occur in plugin-based
architectures.

### Native profiler: conflicting version ordering

The native profiler processes assembly references in module-load order.
If an earlier module has been redirected to the instrumentation's version
and a later module references a **higher** version than the
instrumentation ships, the profiler cannot reconcile the two. This is
logged as an error. In practice this is rare because the instrumentation
ships the latest versions of its dependencies.

### Unexpected resolution request for a higher version than available

The instrumentation's assembly resolver may receive a resolution
request for a version of a shared dependency that is higher than what
is available — either in the TPA list or shipped by the
instrumentation. This can occur when:

- Application code programmatically loads an assembly with an explicit version
  (for example, `Assembly.Load(new AssemblyName("..., Version=X.Y.Z.W"))`)
- A plugin loaded by the application has different version requirements
- A third-party library explicitly requests a specific version via reflection

When this happens, the instrumentation skips the request rather than loading an
older, potentially incompatible assembly. The behavior differs by deployment mode:

- **Native profiler deployment:** The resolver skips the request and logs an
  informational message. The profiler rewrites all metadata `AssemblyRef` tokens,
  so requests that bypass IL rewriting should be quite rare.
- **StartupHook-only deployment:** The isolated ALC skips the request. Because
  there is no IL rewriting in this mode, such requests are more likely to occur
  from the scenarios mentioned above.

**Recommendation:** If you encounter assembly resolution failures
caused by this scenario, the issue lies with application code
requesting a version that is not available. Ensure that the required
version is available to the application — for example, by adding or
upgrading the dependency so that its version is at least what the
requesting code expects. When the required version is available
(either in the TPA list or shipped by the instrumentation), the
resolver will be able to satisfy the request. If that is not possible,
consider the
[`DOTNET_ADDITIONAL_DEPS` approach](#last-resort-dotnet_additional_deps-and-the-runtime-store)
to supply the required version to the runtime before the application
starts.

## Troubleshooting

1. **Enable debug logging** — set `OTEL_LOG_LEVEL=debug`. The assembly
   resolver logs every resolution attempt, including which path and ALC
   was used.
2. **Enable host tracing** — set `COREHOST_TRACE=1` and
   `COREHOST_TRACEFILE=corehost_verbose_tracing.log` to capture the
   runtime's own assembly-loading decisions.
3. **Review the native profiler log** — look for
   `RedirectAssemblyReferences` entries to confirm that IL rewriting
   happened and which versions were involved.

### Last resort: `DOTNET_ADDITIONAL_DEPS` and the runtime store

If the above strategies do not resolve a conflict — for example, the
application uses a framework-level assembly that cannot be redirected
— you can configure the .NET host to include additional dependencies
at startup using
[`DOTNET_ADDITIONAL_DEPS`](https://learn.microsoft.com/dotnet/core/dependency-loading/understanding-assemblyloadcontext#additional-deps)
and
[`DOTNET_SHARED_STORE`](https://learn.microsoft.com/dotnet/core/deploying/runtime-store)
environment variables. This approach makes the runtime aware of the
instrumentation's assemblies before the application starts, avoiding
version conflicts entirely. However, it requires preparing a
`.deps.json` file and potentially a shared store layout — a
significant effort compared to the other deployment options. Consult
the
[.NET documentation](https://learn.microsoft.com/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
for details.
