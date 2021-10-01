using System;
using System.Collections.Generic;
using System.Linq;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace IntegrationTests.Helpers
{
    internal static class DockerNetworkHelper
    {
        public const string IntegrationTestsNetworkName = "integration-tests";
        public const string IntegrationTestsGateway = "10.1.1.1";

        internal static string SetupIntegrationTestsNetwork()
        {
            var client = new DockerClientConfiguration().CreateClient();
            var networks = client.Networks.ListNetworksAsync().Result;
            var network = networks.FirstOrDefault(x => x.Name == IntegrationTestsNetworkName);

            if (network != null)
            {
                if (network.IPAM.Config[0].Gateway != IntegrationTestsGateway)
                {
                    client.Networks.DeleteNetworkAsync(network.ID).Wait();
                }

                return network.ID;
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

            var result = client.Networks.CreateNetworkAsync(networkParams).Result;
            if (string.IsNullOrWhiteSpace(result.ID))
            {
                throw new Exception("Could not create docker network");
            }

            return result.ID;
        }
    }
}
