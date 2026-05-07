# .NET Framework AppDomain Strategy

`OTEL_DOTNET_AUTO_APP_DOMAIN_STRATEGY` is a .NET Framework-only setting
that controls how OpenTelemetry .NET Automatic Instrumentation handles
non-default `AppDomain` creation.

This document focuses on the problem that appears when an application
creates secondary `AppDomain` instances with
`LoaderOptimization.MultiDomain` or `LoaderOptimization.MultiDomainHost`.
The short version is that the CLR may try to share strong-named
assemblies across domains, and that sharing decision can make automatic
instrumentation either incomplete or unstable unless we intervene.

For the broader assembly version-conflict background, see
[Assembly Conflict Resolution](./assembly-conflict-resolution.md).

## Scope of this document

This document explains the problem `OTEL_DOTNET_AUTO_APP_DOMAIN_STRATEGY`
is meant to solve for secondary `AppDomain` creation on .NET Framework.
It intentionally focuses on the high-level runtime behavior rather than
every CLR binding detail.

In particular, the same family of issues appears with both
`MultiDomainHost` and `MultiDomain`. The exact shape depends on GAC
installation, redirects, probing, and which versions exist in the
application's dependency tree, but the root issue is the same:
secondary domains can cause the CLR to share or split strong-named
assemblies in ways that affect instrumentation correctness.

## Why secondary AppDomains are special

On .NET Framework, assemblies are normally loaded per `AppDomain`.
However, when the CLR sees a secondary domain created with
`MultiDomain` or `MultiDomainHost`, some strong-named assemblies may be
loaded as **domain-neutral** so they can be shared across domains.

This is useful for memory savings, but it changes the loading rules:

- The CLR performs extra checks to decide whether an assembly can be
  shared between domains.
- The result of that resolution is effectively remembered and reused for
  later loads.
- If the CLR can resolve the same assembly identity in a compatible way,
  it may reuse the existing domain-neutral instance.
- If it resolves to something different, it may create a separate
  instance instead. That separate instance may itself be either
  domain-neutral or domain-specific.

For OpenTelemetry auto-instrumentation, this matters because we do not
only need our own assemblies to load. Bytecode instrumentation
effectively adds an assembly reference from an instrumented assembly
such as `System.Web` to `OpenTelemetry.AutoInstrumentation`, which means
the OpenTelemetry assembly and its dependencies become part of
that instrumented assembly's dependency graph. Secondary `AppDomain`
loading therefore has to keep the instrumented assembly, the
OpenTelemetry assemblies, and their resolved versions compatible.

## What problem the setting solves

The main problem is not ordinary assembly resolution inside the first
`AppDomain`. The hard case is:

1. The default `AppDomain` loads framework assemblies.
2. A secondary `AppDomain` is created with `MultiDomain` or
   `MultiDomainHost`.
3. The CLR decides whether assemblies for that new domain can reuse an
   existing domain-neutral instance or need a different instance.
4. OpenTelemetry assemblies and their dependencies may now resolve
   differently from what bytecode instrumentation expects.

That can lead to several outcomes:

- Instrumentation of methods in domain-neutral GAC assemblies becomes
  impossible because the OpenTelemetry loader assembly was not resolved
  in a compatible way.
- The application ends up with multiple copies of assemblies that were
  expected to be shared.
- An instrumented assembly such as `System.Web` may need another copy
  because one site or domain resolves a different dependency graph.
- If different versions appear in the same dependency hierarchy, the
  process may fail with assembly-load exceptions or even crash.

One concrete failure seen in simple ASP.NET applications is:

`Loading this assembly would produce a different grant set from other instances`

## The available strategies

| Strategy                        | What OpenTelemetry changes                                                                      | Main effect                                                                                   |
|---------------------------------|-------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| `None`                          | No workaround for secondary domains                                                             | Lowest intervention, but secondary multi-domain resolution may block or break instrumentation |
| `LoaderOptimizationSingleDomain`| Forces new non-default domains to use `LoaderOptimization.SingleDomain`                         | Avoids domain-neutral sharing for those new domains                                           |
| `AssemblyRedirect`              | Modifies config for new non-default domains to add binding redirects and `codeBase` information | Tries to keep CLR resolution consistent enough for instrumentation to work                    |

## Strategy: `None`

`None` means OpenTelemetry does not apply a workaround for newly created
non-default domains.

In `None` mode OpenTelemetry does not patch the new domain, so the CLR
is free to perform the domain-neutral sharing logic described above.

### What happens in practice

Consider this simplified example:

- The default domain loads `System.Web` from the GAC.
- A web site creates a secondary domain with `MultiDomainHost`.
- OpenTelemetry wants `System.Web` and the OpenTelemetry assemblies to
  participate in instrumentation for that site.

The CLR now decides whether the secondary domain can reuse what was
already loaded, or whether it needs different instances.

### Case 1: OpenTelemetry assemblies are not in the GAC

If `OpenTelemetry.AutoInstrumentation` is not registered in the GAC,
the CLR cannot resolve it the same way as the already shared GAC-loaded
framework assemblies.

Result:

- The OpenTelemetry assembly is not available as part of the
  domain-neutral shared graph.
- The OpenTelemetry assembly may still be loaded into the application
  `AppDomain` by reflection, but that does not satisfy binding for a
  domain-neutral instrumented assembly such as `System.Web`.
- Methods in GAC assemblies that were loaded as domain-neutral cannot be
  instrumented as expected.
- For this domain-neutral case, OpenTelemetry usually detects the
  problem before rewriting the method body. The native profiler tracks
  whether the managed profiler assembly was loaded into the shared
  domain. If it was not, rewriting for that domain-neutral method is
  skipped and a warning is logged instead of relying on a later managed
  exception path.

### Case 2: OpenTelemetry assemblies are in the GAC

If `OpenTelemetry.AutoInstrumentation` is installed in the GAC, the
domain-neutral binding problem is no longer simply "our assembly cannot
be found." In this case the harder problem is that the resulting
dependency tree may still conflict with what was already resolved for
the shared domain-neutral assembly.

For example:

- The default domain resolved a dependency chain for domain-neutral
  `System.Web`.
- `OpenTelemetry.AutoInstrumentation` is available from the GAC.
- A later secondary domain resolves one dependency in that combined
  hierarchy to a different version.
- The CLR can no longer treat the whole graph as the same shared graph.

Possible outcomes:

- In practice, the main problem is that the runtime tries to keep
  reusing the existing domain-neutral graph and then hits a conflict.
- That conflict can result in load exceptions such as the grant-set
  error above, or in the worst case an application crash.
- Additional instances may still be loaded in some cases, potentially as
  domain-specific assemblies, but that is a secondary issue here.

## Strategy: `LoaderOptimizationSingleDomain`

This strategy changes each newly created non-default domain to
`LoaderOptimization.SingleDomain`.

That trades memory sharing for predictability:

- Assemblies are no longer shared between those secondary domains.
- Each new domain loads its own copy of the assemblies it uses,
  including not only instrumented assemblies but other assemblies loaded
  into that domain as well.
- The CLR no longer needs to keep reusing the same domain-neutral copy
  across unrelated domains.

This is the simplest workaround because it avoids the hardest part of
the problem instead of trying to steer it.

### Example

In a web application:

- `System.Web` loaded for the first domain remains domain-neutral.
- Each web application domain created later loads its own
  domain-specific `System.Web`.
- The same applies to OpenTelemetry assemblies and other GAC assemblies
  involved in the instrumentation path: in those later application
  domains they are loaded as domain-specific, not domain-neutral.

### Tradeoff

This mode is usually easier to reason about, but it gives up the memory
benefit of domain-neutral sharing for those non-default domains.

Another tradeoff is that a single non-default `AppDomain` may still end
up loading multiple versions of OpenTelemetry dependency assemblies
through reflection-based loads. That is usually not desirable, but this
strategy does not try to unify those loads with binding redirects.

## Strategy: `AssemblyRedirect`

This strategy keeps the multi-domain model, but patches the new
domain's configuration so the CLR has explicit binding information for
assemblies in the OpenTelemetry dependency set.

OpenTelemetry adds or updates:

- `bindingRedirect`
- `codeBase`
- probing information derived from the new domain's setup

The goal is to make assembly resolution deterministic enough that the
CLR can load the right assembly graph for the new domain instead of
discovering conflicting versions later.

This also helps with reflection-based loads inside the same `AppDomain`.
Unlike `LoaderOptimizationSingleDomain`, `AssemblyRedirect` gives the
CLR binding policy that can guide those loads toward the redirected
version instead of allowing multiple versions of the same dependency to
appear more easily in one application domain.

### When OpenTelemetry assemblies are not in the GAC

If OpenTelemetry assemblies are not installed in the GAC:

- The CLR will not reuse the existing domain-neutral instances for the
  affected instrumented assemblies.
- For that application domain, the instrumented assemblies are loaded as
  domain-specific assemblies, similar to the
  `LoaderOptimizationSingleDomain` strategy.
- The redirected application domain also loads the OpenTelemetry
  assemblies inside that domain.
- Non-instrumented GAC assemblies that do not have direct or indirect
  dependencies on the instrumented assemblies can still reuse the
  existing domain-neutral version.
- This still allows instrumentation to work, but the assemblies are not
  shared as one neutral copy across domains.

### When OpenTelemetry assemblies are in the GAC

If OpenTelemetry assemblies are installed in the GAC:

- OpenTelemetry assemblies can be loaded once as domain-neutral and
  reused.
- `System.Web` end up with more than one domain-neutral instance.

The simplest redirected ASP.NET shape looks like this:

| Assembly                                                               | Possible result                                              |
|------------------------------------------------------------------------|--------------------------------------------------------------|
| `OpenTelemetry.AutoInstrumentation` and its GAC-available dependencies | One domain-neutral instance                                  |
| `System.Web` for domains without redirect                              | One domain-neutral instance serving the default domain       |
| `System.Web` for domains with redirect                                 | Another domain-neutral instance serving the redirected graph |

So even with GAC installation, `AssemblyRedirect` does not mean
"everything becomes one shared copy." It means the CLR gets enough
information to build a compatible graph for each group of domains.

### If the application brings newer dependencies

This is where the behavior gets more subtle.

If the application uses assemblies that are also in the OpenTelemetry
dependency tree, but at newer versions:

- `AssemblyRedirect` can be better than
  `LoaderOptimizationSingleDomain` in this situation because the binding
  policy is visible to the CLR when those assemblies are resolved,
  including reflection-based loads.
- This can help when the application serves a newer dependency version
  than the profiler's AssemblyRef patcher was able to anticipate early
  enough.

- If those newer versions are in the GAC, the CLR may create additional
  domain-neutral `System.Web` instances for each distinct redirected
  graph.
- If those newer versions are not in the GAC, `System.Web` and other
  instrumented assemblies for that web site typically become
  domain-specific instead.

## How to think about the choice

Use this rule of thumb:

- Choose `LoaderOptimizationSingleDomain` when reliability matters more
  than cross-domain sharing.
- Choose `AssemblyRedirect` when you want to preserve the multi-domain
  model and provide the CLR enough binding information to keep the
  assembly graph consistent.
- Choose `None` only as a last resort, after extensive testing, when the
  other options are not suitable or introduce unacceptable issues for
  the application. In that mode you accept the risk that secondary
  `AppDomain` creation may make instrumentation incomplete or unstable.
