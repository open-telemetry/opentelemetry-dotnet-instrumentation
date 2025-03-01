// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace OpenTelemetry.Tracing;

internal sealed class BufferedSpan : IBufferedTelemetry<BufferedSpan>
{
    private readonly SpanInfo _Info;
    private readonly List<KeyValuePair<string, object?>>? _Attributes;
    private readonly List<SpanLink>? _Links;
    private readonly List<SpanEvent>? _Events;

    public BufferedSpan(in Span span)
    {
        _Info = span.Info;

        _Attributes = [.. span.Attributes];
        _Links = [.. span.Links];
        _Events = [.. span.Events];
    }

    public InstrumentationScope Scope => _Info.Scope;

    public BufferedSpan? Next { get; set; }

    public ref readonly SpanInfo Info => ref _Info;

    public void ToSpan(out Span span)
    {
        span = new Span(in _Info)
        {
            Attributes = CollectionsMarshal.AsSpan(_Attributes),
            Links = CollectionsMarshal.AsSpan(_Links),
            Events = CollectionsMarshal.AsSpan(_Events)
        };
    }
}
