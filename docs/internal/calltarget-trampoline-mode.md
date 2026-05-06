# CallTarget Trampoline Mode

## Summary

CallTarget trampoline mode is an opt-in .NET Framework-only rewrite mode for bytecode instrumentation. It is enabled with `OTEL_DOTNET_AUTO_CALLTARGET_TRAMPOLINE_ENABLED=true`.

The mode avoids adding `OpenTelemetry.AutoInstrumentation` references to rewritten target assemblies and to `mscorlib`. Target methods call generated generic trampoline holder types in `mscorlib`; each closed generic holder initializes a static delegate once and then invokes that delegate directly.

## V2 Design

- Generate trampoline infrastructure in `mscorlib` during the existing .NET Framework corlib `ModuleLoadFinished` path.
- Keep generated `mscorlib` metadata limited to `mscorlib` types. Do not emit an `OpenTelemetry.AutoInstrumentation` `AssemblyRef` or `TypeRef`.
- Generate `__OTelCallTargetIndexer__<T>` and use nested `Indexer<...<object>>` shapes as global integration map keys.
- Generate mscorlib vessel structs: `__OTelCallTargetState__`, `__OTelCallTargetReturn__`, and `__OTelCallTargetReturn__<TReturn>`.
- Generate Begin holders for fast arities 0 through 8, a slow `object[]` Begin holder, End/EndVoid holders, LogException holders, and matching delegate types.
- Put delegate creation in the managed profiler assembly. Generated `mscorlib` static constructors only locate `CallTargetTrampolineInvoker` by reflection and call a public factory method once.
- The managed factory resolves the global map key through native P/Invoke, creates typed dynamic-method delegates, and converts between mscorlib vessels and real CallTarget state/return structs.

## Rewrite Rules

- Rewritten target methods emit only `mscorlib` TypeRefs/TypeSpecs/MethodSpecs and skip `GetIntegrationTypeRef()`.
- Fast Begin supports 0..8 arguments and passes every argument by reference, matching direct CallTarget fast-path shape.
- Slow Begin supports 9+ arguments via `object[]`; slow-path by-ref arguments remain unsupported.
- End keeps the direct CallTarget shape: store a mscorlib `CallTargetReturn` vessel, call `GetReturnValue()` for non-void methods, and store the resulting return local.
- By-ref return behavior intentionally mirrors the direct CallTarget branch. It is not specially skipped in trampoline mode.
- Trace-method instrumentation remains outside trampoline mode.

## Cache Rule

Native keeps one global index per unique integration type. The generated `TMapIntegration` type encodes that index as nesting depth, where index `0` is `Indexer<object>`, index `1` is `Indexer<Indexer<object>>`, and so on.

Current native ReJIT state still stores one selected `IntegrationDefinition` per `(module, mdMethodDef)`, preserving first-match behavior. Per-target local indexing is deferred as a future optimization.

## Scope

V2 targets .NET Framework only. .NET/Core support is deferred; possible follow-ups include a `System.Runtime` host/type-forwarding strategy or a carefully scoped `System.Private.CoreLib` host.

When trampoline generation fails, trampoline-mode rewrites should be skipped instead of falling back to direct CallTarget rewriting under the trampoline option.
