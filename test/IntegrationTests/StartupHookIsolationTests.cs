// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !NETFRAMEWORK
using System.Security.Cryptography;
using IntegrationTests.Helpers;
using OpenTelemetry.AutoInstrumentation;
using Xunit.Abstractions;

namespace IntegrationTests;

public class StartupHookIsolationTests(ITestOutputHelper output) : TestHelper("StartupHookIsolation", output)
{
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void IsolationRedirectsExitCode_Success()
    {
        // Arrange
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "StartupHookIsolation.ActivitySource");
        // Test application sends a span with the name of the AssemblyLoadContext where the executing assembly is loaded.
        // This serves as a validation of successful isolation
        collector.Expect("StartupHookIsolation.ActivitySource", span => span.Name == StartupHookConstants.IsolatedAssemblyLoadContextName);

        // Run test application and pass expected exit code and expect that the application exits with the same code.
        // We use random exit code (within expected range [1, 99]) as a validation that isolation do not alter original exit call.
        // The application should not crash as a validation that the entrypoint is not executed twice
        var exitCode = RandomNumberGenerator.GetInt32(1, 100);
        var (standardOutput, _, _) = RunTestApplication(
            new TestSettings { Arguments = $"{exitCode}" },
            expectedExitCode: exitCode);

        // Assert
        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void IsolationRedirectsExitCode_Throw()
    {
        // Arrange
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        // make sure Otel spans are flushed even when application throws unhandled exception
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "StartupHookIsolation.ActivitySource");
        // Test application sends a span with the name of the AssemblyLoadContext where the executing assembly is loaded.
        // This serves as a validation of successful isolation
        collector.Expect("StartupHookIsolation.ActivitySource", span => span.Name == StartupHookConstants.IsolatedAssemblyLoadContextName);

        // Run test application and pass expected exception to be thrown (type and message)
        // and expect the same exception to be thrown as a validation that isolation do not hide the original exception.
        // The application should not crash with another exception that serves as a validation that the entry point is not executed twice
        var (standardOutput, errorOutput, _) = RunTestApplication(
            new TestSettings { Arguments = "throw System.Exception Expected test exception" },
            expectedExitCode: 0,
            assertExitCode: Assert.NotEqual);

        // Assert
        collector.AssertExpectations();
        Assert.Contains("System.Exception: Expected test exception", errorOutput, StringComparison.CurrentCulture);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void NoIsolationWhenRuleValidationFails_NoFailFast()
    {
        // Arrange
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        // Trigger rule validation failure
        // ApplicationInExcludeListRule will fail when process is excluded
        // Exclude dotnet executable on both Unix and Windows platforms
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", "dotnet,dotnet.exe");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "StartupHookIsolation.ActivitySource");

        // Run test application normally and expect normal exit code 0 which serves as a validation,
        // that when a rule fails and we don't configure failFast, the isolation do not interfere
        // with normal run and the application runs in Default AssemblyLoadContext.
        // The application should not crash as a validation that the entrypoint is not executed twice
        var (standardOutput, errorOutput, processId) = RunTestApplication(expectedExitCode: 0);

        // Assert
        collector.AssertEmpty();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void NoIsolationWhenRuleValidationFails_FailFast()
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
        var (standardOutput, errorOutput, processId) = RunTestApplication(expectedExitCode: 0, assertExitCode: Assert.NotEqual);

        // Assert
        collector.AssertEmpty();
    }
}
#endif
