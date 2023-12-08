// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Docker.DotNet;
using Docker.DotNet.Models;

namespace IntegrationTests.Helpers;

/// <summary>
/// Helper to call Docker network API
/// </summary>
internal static class DockerNetworkHelper
{
    public const string IntegrationTestsGateway = "10.1.1.1";
    private const string IntegrationTestsNetworkName = "integration-tests";

    /// <summary>
    /// Creates a new Docker network with fixed name and gateway.
    /// if named network exists with specified fixed gateway address, gets the existing one.
    /// </summary>
    /// <returns>Docker network name</returns>
    internal static async Task<string> SetupIntegrationTestsNetworkAsync()
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

        var networkParams = new NetworksCreateParameters
        {
            Name = IntegrationTestsNetworkName,
            Driver = "nat",
            IPAM = new IPAM
            {
                Config = new List<IPAMConfig>()
            }
        };

        networkParams.IPAM.Config.Add(new IPAMConfig
        {
            Gateway = IntegrationTestsGateway,
            Subnet = "10.1.1.0/24"
        });

        var result = await client.Networks.CreateNetworkAsync(networkParams);
        if (string.IsNullOrWhiteSpace(result.ID))
        {
            throw new Exception("Could not create docker network");
        }

        return IntegrationTestsNetworkName;
    }
}
