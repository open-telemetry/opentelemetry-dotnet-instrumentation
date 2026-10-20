// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestLibrary.InstrumentationTarget;

public class Command
{
    public bool WrapBubbleUpExceptions { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable CA1822 // Mark members as static
    public void Execute()
    {
        Thread.Yield(); // Just to have some call to outside code.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void BubbleUpOnBegin()
    {
        Thread.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void BubbleUpOnEnd()
    {
        Thread.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void IgnoreRegularIntegrationException()
    {
        Thread.Yield();
    }

#if NET
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UseSpan(Span<int> value)
    {
        value[0]++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UseReadOnlySpan(ReadOnlySpan<int> value) => Console.WriteLine(value[0]);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UseByRefSpan(ref Span<int> value)
    {
        value[0]++;
    }
#endif

    public void InstrumentationTargetMissingBytecodeInstrumentationType()
    {
        Thread.Sleep(0); // Just to have some call to outside code.
    }

    public void InstrumentationTargetMissingBytecodeInstrumentationMethod()
    {
        Thread.Yield(); // Just to have some call to outside code.
#pragma warning restore CA1822 // Mark members as static
    }
}
