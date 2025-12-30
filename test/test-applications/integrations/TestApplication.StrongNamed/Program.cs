// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using TestApplication.Shared;
using TestLibrary.InstrumentationTarget;

namespace TestApplication.StrongNamed;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var command = new Command();
        command.Execute();
        command.InstrumentationTargetMissingBytecodeInstrumentationType();
        command.InstrumentationTargetMissingBytecodeInstrumentationMethod();
    }
}
