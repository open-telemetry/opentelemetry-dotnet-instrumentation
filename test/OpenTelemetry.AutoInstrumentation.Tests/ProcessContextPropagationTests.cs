// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Process;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class ProcessContextPropagationTests
{
    private const string TraceParent = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01";
    private const string TraceState = "vendor=value";

    [Theory]
    [InlineData("traceparent", "TRACEPARENT")]
    [InlineData("x-b3-traceid", "X_B3_TRACEID")]
    [InlineData("", "_")]
    [InlineData("1abc", "_1ABC")]
    [InlineData("héad.er", "H_AD_ER")]
    [InlineData("_ALREADY_123", "_ALREADY_123")]
    public void NormalizesCarrierKeys(string key, string expected)
    {
        Assert.Equal(expected, EnvironmentCarrier.NormalizeKey(key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CarrierLookupUsesPlatformComparisonWithoutNormalizingExistingNames(bool caseInsensitive)
    {
        var carrier = new Dictionary<string, string?>(caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
        {
            ["traceparent"] = TraceParent,
            ["x-b3-traceid"] = "unusable"
        };

        Assert.Equal(caseInsensitive ? [TraceParent] : Array.Empty<string>(), EnvironmentCarrier.Get(carrier, "traceparent"));
        Assert.Empty(EnvironmentCarrier.Get(carrier, "x-b3-traceid"));
        EnvironmentCarrier.Set(carrier, "x-b3-traceid", "normalized");
        Assert.Equal("normalized", carrier["X_B3_TRACEID"]);
    }

    [Fact]
    public void InvocationParentPreservesRemoteContextFlagsAndTraceState()
    {
        var propagation = CreatePropagation();
        ActivityContext context = default;
        IEnumerable<ActivityLink>? links = null;

        propagation.ApplyToInvocation(ref context, ref links);

        Assert.True(context.IsRemote);
        Assert.Equal(ActivityTraceFlags.Recorded, context.TraceFlags);
        Assert.Equal(TraceState, context.TraceState);
        Assert.Equal("0123456789abcdef0123456789abcdef", context.TraceId.ToHexString());
        Assert.Equal("0123456789abcdef", context.SpanId.ToHexString());
        Assert.Null(links);
    }

    [Theory]
    [InlineData((int)ProcessContextExtractionMode.Parent)]
    [InlineData((int)ProcessContextExtractionMode.Link)]
    [InlineData((int)ProcessContextExtractionMode.Off)]
    public void PublicInvocationCreationSuppliesContextBeforeSampling(int mode)
    {
        var propagation = CreatePropagation((ProcessContextExtractionMode)mode);
        using var source = new ActivitySource(nameof(PublicInvocationCreationSuppliesContextBeforeSampling));
        ActivityContext sampledParent = default;
        ActivityLink[] sampledLinks = [];
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => ReferenceEquals(candidate, source),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                sampledParent = options.Parent;
                sampledLinks = options.Links?.ToArray() ?? [];
                return ActivitySamplingResult.AllDataAndRecorded;
            }
        };
        ActivitySource.AddActivityListener(listener);
        ActivityContext parentContext = default;
        IEnumerable<ActivityLink>? links = null;
        propagation.ApplyToInvocation(ref parentContext, ref links);

        using var invocation = source.StartActivity("invocation", ActivityKind.Internal, parentContext, links: links);

        Assert.NotNull(invocation);
        if (mode == (int)ProcessContextExtractionMode.Parent)
        {
            Assert.True(sampledParent.IsRemote);
            Assert.Equal(TraceState, sampledParent.TraceState);
            Assert.Equal("0123456789abcdef", invocation.ParentSpanId.ToHexString());
            Assert.Equal(sampledParent.TraceId, invocation.TraceId);
            Assert.Empty(sampledLinks);
        }
        else
        {
            Assert.Equal(default, sampledParent);
            Assert.Equal(default, invocation.ParentSpanId);
            if (mode == (int)ProcessContextExtractionMode.Link)
            {
                Assert.Equal("0123456789abcdef", Assert.Single(sampledLinks).Context.SpanId.ToHexString());
                Assert.Equal(sampledLinks, invocation.Links);
            }
            else
            {
                Assert.Empty(sampledLinks);
            }
        }
    }

    [Fact]
    public void LinkModePreservesExistingLinksAndDoesNotDuplicateIncomingContext()
    {
        var propagation = CreatePropagation(ProcessContextExtractionMode.Link);
        var existing = new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None));
        ActivityContext context = default;
        IEnumerable<ActivityLink>? links = [existing];

        propagation.ApplyToInvocation(ref context, ref links);
        propagation.ApplyToInvocation(ref context, ref links);

        Assert.Equal(default, context);
        Assert.NotNull(links);
        Assert.Equal(2, links.Count());
        Assert.Equal(existing.Context, links.First().Context);
        Assert.Equal("0123456789abcdef", links.Last().Context.SpanId.ToHexString());
    }

    [Fact]
    public void PreservesExplicitAndAmbientParents()
    {
        var propagation = CreatePropagation();
        var explicitContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None);
        var context = explicitContext;
        IEnumerable<ActivityLink>? links = null;
        propagation.ApplyToInvocation(ref context, ref links);
        Assert.Equal(explicitContext, context);

        using var ambient = new Activity("ambient");
        ambient.Start();
        context = default;
        propagation.ApplyToInvocation(ref context, ref links);
        Assert.Equal(default, context);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("00-00000000000000000000000000000000-0123456789abcdef-01")]
    public void InvalidIncomingTraceContextDoesNotChangeInvocations(string traceParent)
    {
        var propagation = new ProcessContextPropagation(ProcessContextExtractionMode.Parent, new TraceContextPropagator(), new Dictionary<string, string?> { ["TRACEPARENT"] = traceParent });
        ActivityContext context = default;
        IEnumerable<ActivityLink>? links = null;

        propagation.ApplyToInvocation(ref context, ref links);

        Assert.Equal(default, context);
        Assert.Null(links);
    }

    [Fact]
    public void OffModeDoesNotExtractTraceContextOrBaggage()
    {
        var previousBaggage = Baggage.Current;
        try
        {
            Baggage.Current = default;
            var propagation = CreatePropagation(ProcessContextExtractionMode.Off, "startup=value");
            propagation.InitializeBaggage();
            ActivityContext context = default;
            IEnumerable<ActivityLink>? links = null;
            propagation.ApplyToInvocation(ref context, ref links);

            Assert.Equal(default, context);
            Assert.Empty(Baggage.Current.GetBaggage());
        }
        finally
        {
            Baggage.Current = previousBaggage;
        }
    }

    [Fact]
    public async Task StartupBaggageHonorsExistingValuesAndFlowsToTasks()
    {
        var previousBaggage = Baggage.Current;
        try
        {
            Baggage.Current = Baggage.Create(new Dictionary<string, string> { ["existing"] = "application" });
            var propagation = CreatePropagation(baggage: "startup=value,existing=environment");
            propagation.InitializeBaggage();

            Assert.Equal("application", Baggage.Current.GetBaggage("existing"));
            Assert.Equal("value", await Task.Run(() => Baggage.Current.GetBaggage("startup")));
            Baggage.SetBaggage("startup", "updated");
            var environment = new Dictionary<string, string?>();
            propagation.InjectEnvironment(environment);
            Assert.Contains("startup=updated", environment["BAGGAGE"], StringComparison.Ordinal);
        }
        finally
        {
            Baggage.Current = previousBaggage;
        }
    }

    [Fact]
    public void BaggageOnlyExtractionWorksWithoutAValidTraceParent()
    {
        var previousBaggage = Baggage.Current;
        try
        {
            Baggage.Current = default;
            var propagation = new ProcessContextPropagation(ProcessContextExtractionMode.Link, new BaggagePropagator(), new Dictionary<string, string?> { ["BAGGAGE"] = "startup=value" });
            propagation.InitializeBaggage();
            Assert.Equal("value", Baggage.Current.GetBaggage("startup"));
        }
        finally
        {
            Baggage.Current = previousBaggage;
        }
    }

    [Fact]
    public void InjectionUsesTheActiveSpanAndClearsStaleOptionalFields()
    {
        var previousBaggage = Baggage.Current;
        try
        {
            Baggage.Current = default;
            var propagation = CreatePropagation();
            using var activity = new Activity("active");
            activity.SetIdFormat(ActivityIdFormat.W3C).Start();
            var environment = new Dictionary<string, string?> { ["TRACEPARENT"] = TraceParent, ["TRACESTATE"] = TraceState, ["BAGGAGE"] = "stale=value", ["CUSTOM"] = "untouched" };

            propagation.InjectEnvironment(environment);

            Assert.Equal(activity.Id, environment["TRACEPARENT"]);
            Assert.False(environment.ContainsKey("TRACESTATE"));
            Assert.False(environment.ContainsKey("BAGGAGE"));
            Assert.Equal("untouched", environment["CUSTOM"]);
        }
        finally
        {
            Baggage.Current = previousBaggage;
        }
    }

    [Theory]
    [InlineData((int)ProcessContextExtractionMode.Parent, true)]
    [InlineData((int)ProcessContextExtractionMode.Link, false)]
    [InlineData((int)ProcessContextExtractionMode.Off, false)]
    public void InjectionWithoutAnActivityForwardsIncomingContextOnlyInParentMode(int mode, bool forwards)
    {
        var propagation = CreatePropagation((ProcessContextExtractionMode)mode);
        var environment = new Dictionary<string, string?> { ["TRACEPARENT"] = "stale" };

        propagation.InjectEnvironment(environment);

        Assert.Equal(forwards, environment.ContainsKey("TRACEPARENT"));
        if (forwards)
        {
            Assert.Equal(TraceParent, environment["TRACEPARENT"]);
            Assert.Equal(TraceState, environment["TRACESTATE"]);
        }
    }

    [Fact]
    public void ConfiguredB3PropagatorUsesNormalizedEnvironmentKeys()
    {
        var propagator = new Extensions.Propagators.B3Propagator(singleHeader: false);
        var incoming = new Dictionary<string, string?>
        {
            ["X_B3_TRACEID"] = "0123456789abcdef0123456789abcdef",
            ["X_B3_SPANID"] = "0123456789abcdef",
            ["X_B3_SAMPLED"] = "1"
        };
        var propagation = new ProcessContextPropagation(ProcessContextExtractionMode.Parent, propagator, incoming);
        var outgoing = new Dictionary<string, string?>();

        propagation.InjectEnvironment(outgoing);

        Assert.Equal(incoming, outgoing);
    }

    private static ProcessContextPropagation CreatePropagation(ProcessContextExtractionMode mode = ProcessContextExtractionMode.Parent, string? baggage = null)
    {
        var environment = new Dictionary<string, string?> { ["TRACEPARENT"] = TraceParent, ["TRACESTATE"] = TraceState, ["BAGGAGE"] = baggage };
        var propagator = new CompositeTextMapPropagator([new TraceContextPropagator(), new BaggagePropagator()]);
        return new ProcessContextPropagation(mode, propagator, environment);
    }
}
