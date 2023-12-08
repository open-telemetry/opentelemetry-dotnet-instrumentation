// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestLibrary.InstrumentationTarget;

public class Command
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Execute()
    {
        Thread.Yield(); // Just to have some call to outside code.
    }

    public void InstrumentationTargetMissingBytecodeInstrumentationType()
    {
        Thread.Sleep(0); // Just to have some call to outside code.
    }

    public void InstrumentationTargetMissingBytecodeInstrumentationMethod()
    {
        Thread.Yield(); // Just to have some call to outside code.
    }
}
