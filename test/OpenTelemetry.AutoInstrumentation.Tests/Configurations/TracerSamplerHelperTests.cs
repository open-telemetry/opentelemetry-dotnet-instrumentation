// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class TracerSamplerHelperTests
{
    [Theory]
    [InlineData("always_on", null, "AlwaysOnSampler")]
    [InlineData("always_off", null, "AlwaysOffSampler")]
    [InlineData("traceidratio", null, "TraceIdRatioBasedSampler{1.000000}")]
    [InlineData("traceidratio", "2", "TraceIdRatioBasedSampler{1.000000}")]
    [InlineData("traceidratio", "-1", "TraceIdRatioBasedSampler{1.000000}")]
    [InlineData("traceidratio", "non-a-number", "TraceIdRatioBasedSampler{1.000000}")]
    [InlineData("traceidratio", "0.25", "TraceIdRatioBasedSampler{0.250000}")]
    [InlineData("parentbased_always_on", null, "ParentBased{AlwaysOnSampler}")]
    [InlineData("parentbased_always_off", null, "ParentBased{AlwaysOffSampler}")]
    [InlineData("parentbased_traceidratio", null, "ParentBased{TraceIdRatioBasedSampler{1.000000}}")]
    [InlineData("parentbased_traceidratio", "0.25", "ParentBased{TraceIdRatioBasedSampler{0.250000}}")]
    public void GetSamplerSupportedValues(string tracesSampler, string? tracerSamplerArguments, string expectedDescription)
    {
        var sampler = TracerSamplerHelper.GetSampler(tracesSampler, tracerSamplerArguments);

        sampler.Should().NotBeNull();
        sampler!.Description.Should().Be(expectedDescription);
    }

    [Fact]
    public void GetSamplerNonSupportedValues()
    {
        var sampler = TracerSamplerHelper.GetSampler("non-supported-value", null);

        sampler.Should().BeNull();
    }
}
