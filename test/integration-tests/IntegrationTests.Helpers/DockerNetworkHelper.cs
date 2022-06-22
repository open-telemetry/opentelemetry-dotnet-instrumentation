// <copyright file="DockerNetworkHelper.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace IntegrationTests.Helpers;

/// <summary>
/// Helper to call Docker network API
/// </summary>
internal static class DockerNetworkHelper
{
    public const string IntegrationTestsNetworkName = "integration-tests";
    public const string IntegrationTestsGateway = "10.1.1.1";

    /// <summary>
    /// Creates a new Docker network with fixed name and gateway.
    /// if named network exists with specified fixed gateway address, gets the existing one.
    /// </summary>
    /// <returns>Docker network ID</returns>
    internal static async Task<string> SetupIntegrationTestsNetwork()
    {
        var client = new DockerClientConfiguration().CreateClient();
        var networks = await client.Networks.ListNetworksAsync();
        var network = networks.FirstOrDefault(x => x.Name == IntegrationTestsNetworkName);

        if (network != null)
        {
            if (network.IPAM.Config[0].Gateway == IntegrationTestsGateway)
            {
                return network.ID;
            }
            else
            {
                await client.Networks.DeleteNetworkAsync(network.ID);
            }
        }

        var networkParams = new NetworksCreateParameters()
        {
            Name = IntegrationTestsNetworkName,
            Driver = "nat",
            IPAM = new IPAM()
            {
                Config = new List<IPAMConfig>()
            }
        };

        networkParams.IPAM.Config.Add(new IPAMConfig()
        {
            Gateway = IntegrationTestsGateway,
            Subnet = "10.1.1.0/24"
        });

        var result = await client.Networks.CreateNetworkAsync(networkParams);
        if (string.IsNullOrWhiteSpace(result.ID))
        {
            throw new Exception("Could not create docker network");
        }

        return result.ID;
    }
}
