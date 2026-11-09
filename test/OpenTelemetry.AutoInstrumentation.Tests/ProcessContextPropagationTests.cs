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
    public void RootParentPreservesRemoteContextFlagsAndTraceState()
    {
        var propagation = CreatePropagation();
        ActivityContext context = default;
        IEnumerable<ActivityLink>? links = null;

        propagation.ApplyToRoot(ref context, null, ref links);

        Assert.True(propagation.HasIncomingTraceContext);
        Assert.True(context.IsRemote);
        Assert.Equal(ActivityTraceFlags.Recorded, context.TraceFlags);
        Assert.Equal(TraceState, context.TraceState);
        Assert.Equal("0123456789abcdef0123456789abcdef", context.TraceId.ToHexString());
        Assert.Equal("0123456789abcdef", context.SpanId.ToHexString());
        Assert.Null(links);
    }

    [Fact]
    public void RootContextUsesTheStartupSnapshot()
    {
        var environment = new Dictionary<string, string?> { ["TRACEPARENT"] = TraceParent, ["TRACESTATE"] = TraceState };
        var propagation = new ProcessContextPropagation(ProcessContextExtractionMode.Parent, new TraceContextPropagator(), environment);
        environment["TRACEPARENT"] = "00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-00";
        environment["TRACESTATE"] = "changed=value";
        ActivityContext context = default;
        IEnumerable<ActivityLink>? links = null;

        propagation.ApplyToRoot(ref context, null, ref links);

        Assert.Equal("0123456789abcdef0123456789abcdef", context.TraceId.ToHexString());
        Assert.Equal("0123456789abcdef", context.SpanId.ToHexString());
        Assert.Equal(ActivityTraceFlags.Recorded, context.TraceFlags);
        Assert.Equal(TraceState, context.TraceState);
    }

    [Theory]
    [InlineData((int)ProcessContextExtractionMode.Parent, "")]
    [InlineData((int)ProcessContextExtractionMode.Parent, "explicit-parent")]
    [InlineData((int)ProcessContextExtractionMode.Link, "")]
    [InlineData((int)ProcessContextExtractionMode.Link, "explicit-parent")]
    public void PreservesEverySuppliedParentIdIncludingExplicitRoots(int mode, string parentId)
    {
        var propagation = CreatePropagation((ProcessContextExtractionMode)mode);
        ActivityContext context = default;
        IEnumerable<ActivityLink>? links = null;

        propagation.ApplyToRoot(ref context, parentId, ref links);

        Assert.Equal(default, context);
        Assert.Null(links);
    }

    [Fact]
    public void LinkModePreservesExistingLinksAndDoesNotDuplicateIncomingContext()
    {
        var propagation = CreatePropagation(ProcessContextExtractionMode.Link);
        var existing = new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None));
        ActivityContext context = default;
        IEnumerable<ActivityLink>? links = [existing];

        propagation.ApplyToRoot(ref context, null, ref links);
        propagation.ApplyToRoot(ref context, null, ref links);

        Assert.True(propagation.HasIncomingTraceContext);
        Assert.Equal(default, context);
        Assert.NotNull(links);
        Assert.Equal(2, links.Count());
        Assert.Equal(existing.Context, links.First().Context);
        Assert.Equal("0123456789abcdef", links.Last().Context.SpanId.ToHexString());
    }

    [Theory]
    [InlineData((int)ProcessContextExtractionMode.Parent)]
    [InlineData((int)ProcessContextExtractionMode.Link)]
    public void PreservesExplicitAndAmbientParents(int mode)
    {
        var propagation = CreatePropagation((ProcessContextExtractionMode)mode);
        var explicitContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None);
        var context = explicitContext;
        IEnumerable<ActivityLink>? links = null;
        propagation.ApplyToRoot(ref context, null, ref links);
        Assert.Equal(explicitContext, context);
        Assert.Null(links);

        // A non-default context with no parent span can deliberately select a new root.
        var explicitRoot = new ActivityContext(ActivityTraceId.CreateRandom(), default, ActivityTraceFlags.None);
        context = explicitRoot;
        propagation.ApplyToRoot(ref context, null, ref links);
        Assert.Equal(explicitRoot, context);
        Assert.Null(links);

        using var ambient = new Activity("ambient");
        ambient.Start();
        context = default;
        propagation.ApplyToRoot(ref context, null, ref links);
        Assert.Equal(default, context);
        Assert.Null(links);
    }

    [Theory]
    [InlineData((int)ProcessContextExtractionMode.Parent, null)]
    [InlineData((int)ProcessContextExtractionMode.Parent, "invalid")]
    [InlineData((int)ProcessContextExtractionMode.Parent, "00-00000000000000000000000000000000-0123456789abcdef-01")]
    [InlineData((int)ProcessContextExtractionMode.Link, null)]
    [InlineData((int)ProcessContextExtractionMode.Link, "invalid")]
    [InlineData((int)ProcessContextExtractionMode.Link, "00-00000000000000000000000000000000-0123456789abcdef-01")]
    public void InvalidIncomingTraceContextDoesNotChangeRoots(int mode, string? traceParent)
    {
        var environment = new Dictionary<string, string?>();
        if (traceParent is not null)
        {
            environment["TRACEPARENT"] = traceParent;
        }

        var propagation = new ProcessContextPropagation((ProcessContextExtractionMode)mode, new TraceContextPropagator(), environment);
        ActivityContext context = default;
        IEnumerable<ActivityLink>? links = null;

        propagation.ApplyToRoot(ref context, null, ref links);

        Assert.False(propagation.HasIncomingTraceContext);
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
            propagation.ApplyToRoot(ref context, null, ref links);

            Assert.False(propagation.HasIncomingTraceContext);
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
            Assert.False(propagation.HasIncomingTraceContext);
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
