// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Proto.Profiles.V1Development;

namespace TestApplication.ContinuousProfiler;

internal sealed class SampleBuilder
{
    private readonly Sample _sample = new();
    private long? _value;

    public SampleBuilder AddAttribute(int attributeId)
    {
        _sample.AttributeIndices.Add(attributeId);
        return this;
    }

    public SampleBuilder SetValue(long val)
    {
        _value = val;
        return this;
    }

    public SampleBuilder SetLocationRange(int locationsStartIndex, int locationsLength)
    {
        _sample.LocationsStartIndex = locationsStartIndex;
        _sample.LocationsLength = locationsLength;
        return this;
    }

    public SampleBuilder SetLink(int linkId)
    {
        _sample.LinkIndex = linkId;
        return this;
    }

    public Sample Build()
    {
        if (_value.HasValue)
        {
            _sample.Value.Add(_value.Value);
        }

        return _sample;
    }
}
