// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !NETFRAMEWORK
using System.Security.Cryptography;
using IntegrationTests.Helpers;
using OpenTelemetry.AutoInstrumentation;

namespace IntegrationTests;

public class StartupHookIsolationTests(ITestOutputHelper output) : TestHelper("StartupHookIsolation", output)
{
    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("{00000000-0000-0000-0000-000000000001}")]
    public void IsolationWhenOpenTelemetryProfilerIsNotConfigured(string? profilerId)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("CORECLR_PROFILER", profilerId);

        VerifyApplicationLoadContext(StartupHookConstants.IsolatedAssemblyLoadContextName);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void IsolationWhenProfilerIdIsMissingAndOptionalRulesAreDisabled()
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("CORECLR_PROFILER", null);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED", "false");

        VerifyApplicationLoadContext(StartupHookConstants.IsolatedAssemblyLoadContextName);
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData("{918728DD-259F-4A6A-AC2B-B85E1B658318}")]
    [InlineData("{918728dd-259f-4a6a-ac2b-b85e1b658318}")]
    public void NoIsolationWhenOpenTelemetryProfilerIsConfigured(string profilerId)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("CORECLR_PROFILER", profilerId);

        VerifyApplicationLoadContext("Default");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void NoIsolationWhenRedirectionIsDisabledAndAnotherProfilerIsConfigured()
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("CORECLR_PROFILER", "{00000000-0000-0000-0000-000000000001}");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_REDIRECT_ENABLED", "false");

        VerifyApplicationLoadContext("Default");
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData(false)]
    [InlineData(true)]
    public void NoInstrumentationWhenOpenTelemetryProfilerFailsToLoad(bool failFast)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        EnableBytecodeInstrumentation();
        // A lowercase CLSID must still undergo attachment validation.
        SetEnvironmentVariable("CORECLR_PROFILER", "{918728dd-259f-4a6a-ac2b-b85e1b658318}");
        SetEnvironmentVariable("CORECLR_PROFILER_PATH", Path.Combine(EnvironmentHelper.GetNukeBuildOutput(), "missing-profiler"));
        SetEnvironmentVariable("CORECLR_PROFILER_PATH_32", null);
        SetEnvironmentVariable("CORECLR_PROFILER_PATH_64", null);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", failFast ? "true" : "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "StartupHookIsolation.ActivitySource");

        RunTestApplication(expectedExitCode: 0, assertExitCode: failFast ? Assert.NotEqual : Assert.Equal);

        collector.AssertEmpty();
    }

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
            new TestSettings { Arguments = $"--exit-code {exitCode}" },
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
            new TestSettings { Arguments = "--throw --exception-type System.Exception --exception-message \"Expected test exception\"" },
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

    private void VerifyApplicationLoadContext(string expectedContext)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "StartupHookIsolation.ActivitySource");
        collector.Expect("StartupHookIsolation.ActivitySource", span => span.Name == expectedContext);

        // The test application also detects duplicate entrypoint execution.
        RunTestApplication(new TestSettings { Arguments = "--exit-code 42" }, expectedExitCode: 42);

        collector.AssertExpectations();
    }
}
#endif
