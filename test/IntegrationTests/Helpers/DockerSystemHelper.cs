// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Docker.DotNet;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace IntegrationTests.Helpers;

internal sealed class DockerSystemHelper
{
    public static async Task<bool> GetIsWindowsEngineEnabled()
    {
        using var client = GetDockerClient();

        var version = await client.System.GetVersionAsync().ConfigureAwait(false);
        return version.Os.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) > -1;
    }

    private static DockerClient GetDockerClient()
    {
        var dockerEndpointAuthConfig = TestcontainersSettings.OS.DockerEndpointAuthConfig;
        var sessionId = ResourceReaper.DefaultSessionId;

        using (var dockerClientConfiguration = dockerEndpointAuthConfig.GetDockerClientConfiguration(sessionId))
        {
            return dockerClientConfiguration.CreateClient();
        }
    }
}
