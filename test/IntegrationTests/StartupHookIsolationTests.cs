// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !NETFRAMEWORK
using System.Reflection;
using System.Security.Cryptography;
using IntegrationTests.Helpers;
using OpenTelemetry.AutoInstrumentation;
using Xunit.Abstractions;

namespace IntegrationTests;

public class StartupHookIsolationTests(ITestOutputHelper output) : TestHelper("StartupHookIsolation", output)
{
    public static TheoryData<string, string, int, Action<int, int>, string?, string?> IsolationTestData()
    {
        Type[] exceptionTypes = [.. typeof(Exception).Assembly.GetTypes()
            .Where(typeof(Exception).IsAssignableFrom)
            .Where(t => !t.IsAbstract && t.IsPublic)
            .Where(t => t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding, [typeof(string)]) != null)];

        string[] entryPoints = ["Void", "Int", "Task", "TaskInt", "AsyncTask", "AsyncTaskInt"];

        var data = new TheoryData<string, string, int, Action<int, int>, string?, string?>();
        foreach (var it in entryPoints)
        {
            // Success case: random exit code for int-returning methods, no args for void-returning methods
            // We use random exit code (within expected range [1, 99]) as a validation that isolation do not alter original exit call.
            // For non int-returning entry points, we set exit code to 0 which serves as a validation that the application run in isolated ALC
            int? exitCode = (it is "Int" or "TaskInt" or "AsyncTaskInt")
                ? RandomNumberGenerator.GetInt32(1, 100)
                : null;
            data.Add(it, $"{exitCode}", exitCode ?? 0, Assert.Equal, null, null);

            // Exception case: randomize exception type from System.Private.CoreLib for better coverage
            // Expect the same exception to be thrown as a validation that isolation do not hide the original exception
            // and the application did not crash for other reasosns (e.g. wrong parameters or entrypoint executed twice)
            var exceptionType = exceptionTypes[RandomNumberGenerator.GetInt32(exceptionTypes.Length)].FullName!;
            var exceptionMessage = "Test Exception";
            data.Add(it, $"throw {exceptionType} {exceptionMessage}", 0, Assert.NotEqual, exceptionType, exceptionMessage);
        }

        return data;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(IsolationTestData))]
    public void IsolationRedirects(
        string entryPointType,
        string arguments,
        int expectedExitCode,
        Action<int, int> exitCodeAssertion,
        string? exceptionType,
        string? exceptionMessage)
    {
        // Arrange
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "StartupHookIsolation.ActivitySource");

        var expectException = exceptionType is not null && exceptionMessage is not null;
        if (expectException)
        {
            // make sure Otel spans are flushed even when application throws unhandled exception
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION", "true");
        }

        // Test application sends a span with the name of the AssemblyLoadContext where the executing assembly is loaded.
        // This serves as a validation of successful isolation
        collector.Expect("StartupHookIsolation.ActivitySource", span => span.Name == StartupHookConstants.IsolatedAssemblyLoadContextName);

        // Act
        var (standardOutput, errorOutput, _) = RunTestApplication(
            new TestSettings
            {
                PackageVersion = entryPointType,
                Arguments = arguments
            },
            expectedExitCode: expectedExitCode,
            assertExitCode: exitCodeAssertion);

        // Assert
        collector.AssertExpectations();

        if (expectException)
        {
            Assert.Contains(
                errorOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries),
                line => line.Contains(exceptionType!, StringComparison.CurrentCulture) &&
                        line.Contains(exceptionMessage!, StringComparison.CurrentCulture));
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    internal void NoIsolationWhenRuleValidationFails_NoFailFast()
    {
        // Arrange
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        // Trigger rule validation failure
        // ApplicationInExcludeListRule will fail when process is excluded
        // Exclude dotnet executable on both Unix and Windows platforms
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", "dotnet,dotnet.exe");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "StartupHookIsolation.ActivitySource");

        // Run test application normally and expect exit code 999 which serves as a validation,
        // that when a rule fails and we don't configure failFast, the isolation do not interfere
        // with normal run and the application runs in Default AssemblyLoadContext.
        // Not crashing application should also serves as a validation that the entrypoint is not executed twice
        var (standardOutput, errorOutput, processId) = RunTestApplication(
            new TestSettings { PackageVersion = "Int" },
            expectedExitCode: 999);

        // Assert
        collector.AssertEmpty();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    internal void NoIsolationWhenRuleValidationFails_FailFast()
    {
        // Arrange
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        // Trigger rule validation failure
        // ApplicationInExcludeListRule will fail when process is excluded
        // Exclude dotnet executable on both Unix and Windows platforms
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", "dotnet,dotnet.exe");
        // Setup FailFast to make sure isolation respects it
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "true");
        // make sure Otel spans are flushed even when application throws unhandled exception
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "StartupHookIsolation.ActivitySource");

        // Run test application normally and expect that it crashes.
        // This serves as a validation that when a rule validation fails and failFast is set to true,
        // the isolation respects the flag and the application exits prematurely
        // TODO should i check error output? otherwise it may mean that the application exited in defult alc
        var (standardOutput, errorOutput, processId) = RunTestApplication(
            new TestSettings { PackageVersion = "Int" },
            expectedExitCode: 0,
            assertExitCode: Assert.NotEqual);

        // Assert
        collector.AssertEmpty();
        Assert.NotEmpty(errorOutput);
    }
}
#endif
