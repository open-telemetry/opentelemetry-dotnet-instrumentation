// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Process;

internal sealed class ProcessContextPropagation
{
    private readonly ProcessContextExtractionMode _mode;
    private readonly TextMapPropagator _propagator;
    private readonly PropagationContext _incoming;

    internal ProcessContextPropagation(ProcessContextExtractionMode mode, TextMapPropagator propagator, IDictionary<string, string?> environment)
    {
        _mode = mode;
        _propagator = propagator;
        _incoming = mode == ProcessContextExtractionMode.Off ? default : propagator.Extract(default, environment, EnvironmentCarrier.Get);
    }

    internal void InitializeBaggage()
    {
        if (_incoming.Baggage.Count > 0)
        {
            // Startup input follows the SDK's ordinary baggage inheritance thereafter.
            var baggage = _incoming.Baggage;
            foreach (var item in Baggage.Current.GetBaggage())
            {
                baggage = baggage.SetBaggage(item.Key, item.Value);
            }

            Baggage.Current = baggage;
        }
    }

    internal void ApplyToInvocation(ref ActivityContext context, ref IEnumerable<ActivityLink>? links)
    {
        if (_mode == ProcessContextExtractionMode.Off || !IsValid(_incoming.ActivityContext) ||
            Activity.Current is not null || context != default)
        {
            return;
        }

        if (_mode == ProcessContextExtractionMode.Parent)
        {
            context = _incoming.ActivityContext;
            return;
        }

        var updatedLinks = links?.ToList() ?? [];
        if (!updatedLinks.Any(link => link.Context.TraceId == _incoming.ActivityContext.TraceId && link.Context.SpanId == _incoming.ActivityContext.SpanId))
        {
            updatedLinks.Add(new ActivityLink(_incoming.ActivityContext));
        }

        links = updatedLinks;
    }

    internal void InjectEnvironment(IDictionary<string, string?> environment)
    {
        var context = Activity.Current?.Context ?? default;
        if (!IsValid(context))
        {
            context = _mode == ProcessContextExtractionMode.Parent ? _incoming.ActivityContext : default;
        }

        // Clear fields that a propagator may omit (for example, empty tracestate/baggage).
        // Otherwise an inherited startup value can disagree with the active context.
        if (_propagator.Fields is { } fields)
        {
            foreach (var field in fields)
            {
                environment.Remove(EnvironmentCarrier.NormalizeKey(field));
            }
        }

        _propagator.Inject(new PropagationContext(context, Baggage.Current), environment, EnvironmentCarrier.Set);
    }

    private static bool IsValid(ActivityContext context)
    {
        return context.TraceId != default && context.SpanId != default;
    }
}
