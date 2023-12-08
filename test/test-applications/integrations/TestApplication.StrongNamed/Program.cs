// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using TestLibrary.InstrumentationTarget;

namespace TestApplication.StrongNamed;

public static class Program
{
    public static void Main(string[] args)
    {
        var command = new Command();
        command.Execute();
        command.InstrumentationTargetMissingBytecodeInstrumentationType();
        command.InstrumentationTargetMissingBytecodeInstrumentationMethod();
    }
}
