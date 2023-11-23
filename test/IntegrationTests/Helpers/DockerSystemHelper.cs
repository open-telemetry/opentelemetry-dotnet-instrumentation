// <copyright file="DockerSystemHelper.cs" company="OpenTelemetry Authors">
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

using Docker.DotNet;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace IntegrationTests.Helpers;

internal class DockerSystemHelper
{
    public static async Task<bool> GetIsWindowsEngineEnabled()
    {
        using var client = GetDockerClient();

        var version = await client.System.GetVersionAsync();
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
