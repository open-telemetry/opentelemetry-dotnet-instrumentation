// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class DetectorsTests
{
    [Fact]
    public void GetEnabledResourceDetector_ReturnsCorrectEnabledDetectors()
    {
        var detectors = new DotNetDetectors
        {
            AzureAppService = new object(),
            Host = new object(),
            OperatingSystem = null,
            Process = new object(),
            ProcessRuntime = null
        };

#if NET
        detectors.Container = new object();
#endif

        var result = detectors.GetEnabledResourceDetector();

        var expected = new List<ResourceDetector>
        {
            ResourceDetector.AzureAppService,
            ResourceDetector.Host,
            ResourceDetector.Process
        };

#if NET
        expected.Add(ResourceDetector.Container);
#endif

        Assert.Equal(expected.Count, result.Count);
        foreach (var detector in expected)
        {
            Assert.Contains(detector, result);
        }
    }
}
