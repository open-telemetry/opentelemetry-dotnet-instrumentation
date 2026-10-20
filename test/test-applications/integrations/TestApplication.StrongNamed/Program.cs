// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.CallTarget;
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
        command.WrapBubbleUpExceptions = true;
        VerifyBubbleUpException(command.BubbleUpOnBegin, nameof(Command.BubbleUpOnBegin), expectedWrapped: true);
        VerifyBubbleUpException(command.BubbleUpOnEnd, nameof(Command.BubbleUpOnEnd), expectedWrapped: true);
        command.WrapBubbleUpExceptions = false;
        command.IgnoreRegularIntegrationException();
#if NET
        Span<int> span = stackalloc int[] { 40 };
        command.UseSpan(span);
        command.UseReadOnlySpan(span);
        command.UseByRefSpan(ref span);
#endif
        Console.WriteLine("Regular integration exception was swallowed.");
        command.InstrumentationTargetMissingBytecodeInstrumentationType();
        command.InstrumentationTargetMissingBytecodeInstrumentationMethod();
    }

    private static void VerifyBubbleUpException(Action action, string scenario, bool expectedWrapped = false)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (CallTargetBubbleUpException.IsCallTargetBubbleUpException(exception))
        {
            if (expectedWrapped != (exception is InvalidOperationException && exception.InnerException is TargetInvocationException))
            {
                throw new InvalidOperationException($"Unexpected bubble-up exception wrapper from {scenario}.", exception);
            }

            var prefix = expectedWrapped ? "Wrapped bubble-up" : "Bubble-up";
            Console.WriteLine($"{prefix} exception propagated from {scenario}.");
            return;
        }

        throw new InvalidOperationException($"No bubble-up exception propagated from {scenario}.");
    }
}
