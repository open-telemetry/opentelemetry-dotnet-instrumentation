// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Proto.Profiles.V1.Alternatives.PprofExtended;

namespace TestApplication.ContinuousProfiler;

internal class SampleBuilder
{
    private readonly Sample _sample = new();
    private readonly IList<ulong> _locationIds = new List<ulong>();
    private long? _value;

    public SampleBuilder AddAttribute(ulong attributeId)
    {
        _sample.Attributes.Add(attributeId);
        return this;
    }

    public SampleBuilder SetValue(long val)
    {
        _value = val;
        return this;
    }

    public SampleBuilder AddLocationId(ulong locationId)
    {
        _locationIds.Add(locationId);
        return this;
    }

    public SampleBuilder SetLink(ulong linkId)
    {
        _sample.Link = linkId;
        return this;
    }

    public Sample Build()
    {
        _sample.LocationIndex.AddRange(_locationIds);

        if (_value.HasValue)
        {
            _sample.Value.Add(_value.Value);
        }

        return _sample;
    }
}
