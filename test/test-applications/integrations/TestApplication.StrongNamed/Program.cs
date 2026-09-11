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
        VerifyBubbleUpException(command.BubbleUpOnBegin, nameof(Command.BubbleUpOnBegin));
        VerifyBubbleUpException(command.BubbleUpOnEnd, nameof(Command.BubbleUpOnEnd));
        command.IgnoreRegularIntegrationException();
        Console.WriteLine("Regular integration exception was swallowed.");
        command.InstrumentationTargetMissingBytecodeInstrumentationType();
        command.InstrumentationTargetMissingBytecodeInstrumentationMethod();
    }

    private static void VerifyBubbleUpException(Action action, string scenario)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception.GetType().Name.Contains("BubbleUpException", StringComparison.Ordinal))
        {
            Console.WriteLine($"Bubble-up exception propagated from {scenario}.");
            return;
        }

        throw new InvalidOperationException($"No bubble-up exception propagated from {scenario}.");
    }
}
