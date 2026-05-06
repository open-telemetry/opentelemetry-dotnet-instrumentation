# CallTarget Trampoline Mode

## Summary

CallTarget trampoline mode is an opt-in .NET Framework-only rewrite mode for bytecode instrumentation. It is enabled with `OTEL_DOTNET_AUTO_CALLTARGET_TRAMPOLINE_ENABLED=true`.

The mode avoids adding `OpenTelemetry.AutoInstrumentation` references to rewritten target assemblies and to `mscorlib`. Rewritten methods call a generated helper type in `mscorlib`. The generated helper keeps the public trampoline surface and its own metadata limited to `mscorlib` types, then resolves the managed profiler assembly and a managed trampoline invoker by reflection.

## Design

- Generate a public static `__OTelCallTargetTrampoline__` type in `mscorlib` during the existing .NET Framework corlib `ModuleLoadFinished` path.
- Use only `mscorlib` types in trampoline method signatures and generated `mscorlib` metadata. Do not emit an `OpenTelemetry.AutoInstrumentation` `AssemblyRef` or `TypeRef` into `mscorlib`.
- Use the slow path for v1 by passing target arguments as `object[]`.
- Store begin state as `object`, not as an OpenTelemetry type in the target method signature. In v1 the object is the boxed managed `CallTargetState`; a generated mscorlib vessel type can be added later if we need more control over state shape.
- Return final method return values directly from trampoline end calls; do not expose `CallTargetReturn` or `CallTargetReturn<T>` to rewritten target assemblies.
- Resolve `OpenTelemetry.AutoInstrumentation.CallTarget.CallTargetTrampolineInvoker` by reflection from the integration assembly name and call it through `MethodInfo.Invoke`.
- Unwrap `CallTargetReturn<T>` inside the managed trampoline invoker. It is a `ref struct`, so it must not be returned through `MethodInfo.Invoke` or cross an `object` boundary.
- Skip by-ref target methods in v1 trampoline mode.
- The managed trampoline invoker is intentionally small: it receives `Type` objects and `object` values from the generated `mscorlib` trampoline, constructs closed generic calls internally, and delegates to the existing `CallTargetInvoker` APIs.

## Cache Rule

The preferred future cache key is the pair `(RuntimeMethodHandle, RuntimeTypeHandle)`.

Current native ReJIT state stores one `IntegrationDefinition` per `(module, mdMethodDef)`, so trampoline mode preserves that first-match behavior. The first implementation does not add a handler dictionary; it resolves the managed trampoline invoker through reflection on each trampoline call. A later optimization can cache prepared delegates per target method identity, with integration assembly/type data in the cached handler value rather than in the cache key.

## Scope

V1 targets .NET Framework only. .NET/Core support is deferred; possible follow-ups include a `System.Runtime` host/type-forwarding strategy or a carefully scoped `System.Private.CoreLib` host.

When trampoline generation fails, trampoline-mode rewrites should be skipped instead of falling back to direct CallTarget rewriting under the trampoline option.
