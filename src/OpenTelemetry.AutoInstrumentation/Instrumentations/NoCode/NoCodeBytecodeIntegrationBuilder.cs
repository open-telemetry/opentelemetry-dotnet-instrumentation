// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using static OpenTelemetry.AutoInstrumentation.InstrumentationDefinitions;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

internal static class NoCodeBytecodeIntegrationBuilder
{
    private static readonly string AssemblyFullName = typeof(NoCodeBytecodeIntegrationBuilder).Assembly.FullName!;

    // TODO should be parsed from the file configuration
    internal static List<NoCodeEntry> NoCodeEntries { get; } =
    [
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void"], "Span-TestMethod0"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodA", ["System.Void"], "Span-TestMethodA"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodStatic", ["System.Void"], "Span-TestMethodStatic"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.String"], "Span-TestMethod1String"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.Int32"], "Span-TestMethod1Int"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.String", "System.String"], "Span-TestMethod2"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.String", "System.String", "System.String"], "Span-TestMethod3"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod4"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod5"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod6"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod7"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod8"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethod", ["System.Void", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod9"),

        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32"], "Span-ReturningTestMethod0"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethodStatic", ["System.Int32"], "Span-ReturningTestMethodStatic"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.String"], "Span-ReturningTestMethod1String"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.Int32"], "Span-ReturningTestMethod1Int"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.String", "System.String"], "Span-ReturningTestMethod2"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.String", "System.String", "System.String"], "Span-ReturningTestMethod3"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.String", "System.String", "System.String", "System.String"], "Span-ReturningTestMethod4"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-ReturningTestMethod5"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-ReturningTestMethod6"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-ReturningTestMethod7"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-ReturningTestMethod8"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ReturningTestMethod", ["System.Int32", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-ReturningTestMethod9"),

        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task"], "Span-TestMethod0Async"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAAsync", ["System.Threading.Tasks.Task"], "Span-TestMethodAAsync"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodStaticAsync", ["System.Threading.Tasks.Task"], "Span-TestMethodStaticAsync"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.String"], "Span-TestMethod1StringAsync"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.Int32"], "Span-TestMethod1IntAsync"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.String", "System.String"], "Span-TestMethod2Async"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.String", "System.String", "System.String"], "Span-TestMethod3Async"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod4Async"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod5Async"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod6Async"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod7Async"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod8Async"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "TestMethodAsync", ["System.Threading.Tasks.Task", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String", "System.String"], "Span-TestMethod9Async"),

        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "IntTaskTestMethodAsync", ["System.Threading.Tasks.Task`1[System.Int32]"], "Span-IntTaskTestMethodAsync"),
#if NET
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "ValueTaskTestMethodAsync", ["System.Threading.Tasks.ValueTask"], "Span-ValueTaskTestMethodAsync"),
        new("TestApplication.NoCode", "TestApplication.NoCode.NoCodeTestingClass", "IntValueTaskTestMethodAsync", ["System.Threading.Tasks.ValueTask`1[System.Int32]"], "Span-IntValueTaskTestMethodAsync"),
#endif
    ];

    internal static Payload GetNoCodeDefinitions()
    {
        var definitions = new NativeCallTargetDefinition[NoCodeEntries.Count];

        for (var i = 0; i < NoCodeEntries.Count; i++)
        {
            var entry = NoCodeEntries[i];
            definitions[i] = new NativeCallTargetDefinition(
                entry.TargetAssembly,
                entry.TargetType,
                entry.TargetMethod,
                entry.TargetSignatureTypes,
                0,
                0,
                0,
                ushort.MaxValue,
                ushort.MaxValue,
                ushort.MaxValue,
                AssemblyFullName,
                $"OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.NoCodeIntegration{entry.TargetSignatureTypes.Length - 1}");
        }

        return new Payload
        {
            // Fixed Id for definitions payload (to avoid loading same integrations from multiple AppDomains)
            DefinitionsId = "D3B88A224E034D60AC3A923BABEE6B7F",
            Definitions = definitions,
        };
    }
}
