// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using System.Text;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Process;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.Process;

public class ProcessStartInfoClonerTests
{
    [Fact]
    public void PreservesSettingsAndCustomEnvironment()
    {
        var original = new ProcessStartInfo("child", "--example")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8,
            WorkingDirectory = "example-directory",
            CreateNoWindow = true,
        };
        original.Environment.Clear();
        original.Environment.Add("EXAMPLE", "custom-value");
        original.Environment.Add("TRACEPARENT", "original-context");

        var clone = ProcessStartInfoCloner.Prepare(original, environment =>
        {
            environment["TRACEPARENT"] = "current-context";
            environment["BAGGAGE"] = "example=value";
        });

        Assert.NotSame(original, clone);
        Assert.Equal(original.FileName, clone.FileName);
        Assert.Equal(original.Arguments, clone.Arguments);
        Assert.Equal(original.WorkingDirectory, clone.WorkingDirectory);
        Assert.Equal(original.RedirectStandardOutput, clone.RedirectStandardOutput);
        Assert.Equal(original.StandardOutputEncoding, clone.StandardOutputEncoding);
        Assert.Equal(original.CreateNoWindow, clone.CreateNoWindow);
        Assert.Equal(3, clone.Environment.Count);
        Assert.Equal("custom-value", clone.Environment["EXAMPLE"]);
        Assert.Equal("current-context", clone.Environment["TRACEPARENT"]);
        Assert.Equal("example=value", clone.Environment["BAGGAGE"]);
        Assert.Equal(2, original.Environment.Count);
        Assert.Equal("original-context", original.Environment["TRACEPARENT"]);
        Assert.False(original.Environment.ContainsKey("BAGGAGE"));
    }

    [Fact]
    public void PreservesLazyEnvironmentInheritance()
    {
        var original = new ProcessStartInfo("child") { UseShellExecute = false };
        var parentValue = Environment.GetEnvironmentVariable("OTEL_PROCESS_CLONE_TEST");
        var environmentField = GetEnvironmentField();
        Assert.Null(environmentField.GetValue(original));

        var clone = ProcessStartInfoCloner.Prepare(original, environment => environment["OTEL_PROCESS_CLONE_TEST"] = "child-only");

        Assert.Null(environmentField.GetValue(original));
        Assert.Equal("child-only", clone.Environment["OTEL_PROCESS_CLONE_TEST"]);
        Assert.Equal(parentValue, Environment.GetEnvironmentVariable("OTEL_PROCESS_CLONE_TEST"));
    }

    [Fact]
    public void ReusingStartInfoDoesNotRetainInjectedValues()
    {
        var original = new ProcessStartInfo("child") { UseShellExecute = false };
        original.Environment.Clear();
        original.Environment.Add("EXAMPLE", "original");

        var first = ProcessStartInfoCloner.Prepare(original, environment => environment["TRACEPARENT"] = "first");
        var second = ProcessStartInfoCloner.Prepare(original, environment => environment["TRACEPARENT"] = "second");

        Assert.Equal("first", first.Environment["TRACEPARENT"]);
        Assert.Equal("second", second.Environment["TRACEPARENT"]);
        Assert.Single(original.Environment);
        Assert.Equal("original", original.Environment["EXAMPLE"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentUseDoesNotMixContexts(bool customEnvironment)
    {
        var original = new ProcessStartInfo("child") { UseShellExecute = false };
        if (customEnvironment)
        {
            original.Environment.Clear();
        }

        var clones = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(() =>
            ProcessStartInfoCloner.Prepare(original, environment => environment["TRACEPARENT"] = index.ToString(System.Globalization.CultureInfo.InvariantCulture)))));

        Assert.Equal(16, clones.Select(clone => clone.Environment["TRACEPARENT"]).Distinct().Count());
        if (customEnvironment)
        {
            Assert.Empty(original.Environment);
        }
        else
        {
            Assert.Null(GetEnvironmentField().GetValue(original));
        }
    }

    [Fact]
    public void SkipsShellExecution()
    {
        var original = new ProcessStartInfo("child") { UseShellExecute = true };

        var result = ProcessStartInfoCloner.Prepare(original, _ => throw new InvalidOperationException("Injection must not run."));

        Assert.Same(original, result);
        Assert.Null(GetEnvironmentField().GetValue(original));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparationFailurePreservesOriginalEnvironment(bool customEnvironment)
    {
        var original = new ProcessStartInfo("child") { UseShellExecute = false };
        if (customEnvironment)
        {
            original.Environment.Clear();
            original.Environment.Add("TRACEPARENT", "original-context");
        }

        Assert.Throws<InvalidOperationException>(() => ProcessStartInfoCloner.Prepare(original, environment =>
        {
            environment["TRACEPARENT"] = "modified-context";
            throw new InvalidOperationException("Injection failed.");
        }));

        if (customEnvironment)
        {
            Assert.Single(original.Environment);
            Assert.Equal("original-context", original.Environment["TRACEPARENT"]);
        }
        else
        {
            Assert.Null(GetEnvironmentField().GetValue(original));
        }
    }

    [Fact]
    public void PreservesNullEnvironmentValues()
    {
        var original = new ProcessStartInfo("child") { UseShellExecute = false };
        original.Environment.Clear();
        original.Environment.Add("EXPLICITLY_REMOVED", null);

        var clone = ProcessStartInfoCloner.Prepare(original, environment => environment["TRACEPARENT"] = "current-context");

        Assert.True(clone.Environment.ContainsKey("EXPLICITLY_REMOVED"));
        Assert.Null(clone.Environment["EXPLICITLY_REMOVED"]);
        Assert.Single(original.Environment);
        Assert.Null(original.Environment["EXPLICITLY_REMOVED"]);
    }

    [Fact]
    public void PreservesEnvironmentKeyComparison()
    {
        var original = new ProcessStartInfo("child") { UseShellExecute = false };
        original.Environment.Clear();
        original.Environment["EXAMPLE"] = "upper-case";
        original.Environment["example"] = "lower-case";

        var clone = ProcessStartInfoCloner.Prepare(original, _ => { });

        Assert.Equal(original.Environment.Count, clone.Environment.Count);
        Assert.Equal(original.Environment["EXAMPLE"], clone.Environment["EXAMPLE"]);
        Assert.Equal(original.Environment["example"], clone.Environment["example"]);
    }

    private static FieldInfo GetEnvironmentField()
    {
#if NET
        const string fieldName = "_environmentVariables";
#else
        const string fieldName = "environmentVariables";
#endif
        return typeof(ProcessStartInfo).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
    }
}
