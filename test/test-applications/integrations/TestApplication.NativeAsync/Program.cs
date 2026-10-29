// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using TestApplication.Shared;
using TestLibrary.NativeAsync;

namespace TestApplication.NativeAsync;

internal static class Program
{
    private const int RuntimeAsyncMethodImplFlag = 0x2000;

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
        VerifyCompilationModes();

        EnsureEqual(42, await new RuntimeAsyncStructTarget(41).TaskOfTAsync().ConfigureAwait(false));
#if NET10_0_OR_GREATER
        EnsureEqual(false, RuntimeAsyncStructTarget.WasInstrumented);
#else
        EnsureEqual(true, RuntimeAsyncStructTarget.WasInstrumented);
#endif

        var runtimeTarget = new RuntimeAsyncTarget();
        await RuntimeAsyncTarget.TaskAsync().ConfigureAwait(false);
        EnsureEqual(42, await runtimeTarget.TaskOfTAsync().ConfigureAwait(false));
        await runtimeTarget.ValueTaskAsync().ConfigureAwait(false);
        EnsureEqual("runtime", await runtimeTarget.ValueTaskOfTAsync().ConfigureAwait(false));
        EnsureEqual(73, await runtimeTarget.GenericTaskAsync(73).ConfigureAwait(false));

        await ExpectFailureAsync(() => runtimeTarget.FaultedTaskAsync()).ConfigureAwait(false);
        await ExpectFailureAsync(() => runtimeTarget.FaultedTaskOfTAsync()).ConfigureAwait(false);
        await ExpectFailureAsync(async () => await runtimeTarget.FaultedValueTaskAsync().ConfigureAwait(false)).ConfigureAwait(false);
        await ExpectFailureAsync(async () => _ = await runtimeTarget.FaultedValueTaskOfTAsync().ConfigureAwait(false)).ConfigureAwait(false);

        var conventionalTarget = new ConventionalAsyncTarget();
        await ConventionalAsyncTarget.TaskAsync().ConfigureAwait(false);
        EnsureEqual(24, await conventionalTarget.TaskOfTAsync().ConfigureAwait(false));
        await conventionalTarget.ValueTaskAsync().ConfigureAwait(false);
        EnsureEqual("conventional", await conventionalTarget.ValueTaskOfTAsync().ConfigureAwait(false));
        await ExpectFailureAsync(() => conventionalTarget.FaultedTaskAsync()).ConfigureAwait(false);
        await ExpectFailureAsync(async () => _ = await conventionalTarget.FaultedValueTaskOfTAsync().ConfigureAwait(false)).ConfigureAwait(false);
    }

    private static void VerifyCompilationModes()
    {
#if NET10_0_OR_GREATER
        const bool runtimeAsyncExpected = true;
#else
        const bool runtimeAsyncExpected = false;
#endif
        VerifyCompilationMode(typeof(RuntimeAsyncTarget), runtimeAsyncExpected);
        VerifyCompilationMode(typeof(RuntimeAsyncStructTarget), runtimeAsyncExpected);
        VerifyCompilationMode(typeof(ConventionalAsyncTarget), runtimeAsyncExpected: false);
    }

    private static void VerifyCompilationMode(Type targetType, bool runtimeAsyncExpected)
    {
        var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        foreach (var method in methods)
        {
            var isRuntimeAsync = ((int)method.MethodImplementationFlags & RuntimeAsyncMethodImplFlag) != 0;
            if (isRuntimeAsync != runtimeAsyncExpected)
            {
                throw new InvalidOperationException($"{targetType.FullName}.{method.Name} has an unexpected runtime-async flag value.");
            }
        }
    }

    private static async Task ExpectFailureAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Expected the async operation to fail.");
    }

    private static void EnsureEqual<T>(T expected, T actual)
        where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }
}
