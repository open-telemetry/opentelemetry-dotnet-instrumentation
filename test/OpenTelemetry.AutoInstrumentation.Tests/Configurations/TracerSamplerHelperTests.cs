// <copyright file="TracerSamplerHelperTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
