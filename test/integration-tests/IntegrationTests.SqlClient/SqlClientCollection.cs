// <copyright file="SqlClientCollection.cs" company="OpenTelemetry Authors">
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

using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.WaitStrategies;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.SqlClient
{
    [CollectionDefinition(Name)]
    public class SqlClientCollection : ICollectionFixture<SqlClientFixture>
    {
        public const string Name = nameof(SqlClientCollection);
    }

    public class SqlClientFixture : IAsyncLifetime
    {
        private const int DatabasePort = 1433;
        private const string DatabaseImage = "mcr.microsoft.com/mssql/server:2019-CU15-ubuntu-20.04";
        private const string DatabasePassword = "@someThingComplicated1234";

        private readonly bool _shouldLaunchContainer;
        private TestcontainersContainer _container;

        public SqlClientFixture()
        {
            _shouldLaunchContainer = ShouldLaunchContainer();

            Port = _shouldLaunchContainer
                ? TcpPortProvider.GetOpenPort()
                : DatabasePort;
        }

        public int Port { get; }

        public async Task InitializeAsync()
        {
            if (!_shouldLaunchContainer)
            {
                return;
            }

            _container = await LaunchDatabaseContainerAsync(Port);
        }

        public async Task DisposeAsync()
        {
            if (_container != null)
            {
                await ShutdownDatabaseContainerAsync(_container);
            }
        }

        private static bool ShouldLaunchContainer()
        {
            if (!EnvironmentHelper.IsRunningOnCI())
            {
                return true;
            }

            return !EnvironmentTools.IsWindows() && !EnvironmentTools.IsMacOS();
        }

        private static async Task<TestcontainersContainer> LaunchDatabaseContainerAsync(int port)
        {
            var waitForOs = EnvironmentTools.IsWindows()
                ? Wait.ForWindowsContainer()
                : Wait.ForUnixContainer();

            var databaseContainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage(DatabaseImage)
                .WithName($"sql-server-{port}")
                .WithPortBinding(port, DatabasePort)
                .WithEnvironment("SA_PASSWORD", DatabasePassword)
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithWaitStrategy(waitForOs.UntilPortIsAvailable(DatabasePort));

            var container = databaseContainersBuilder.Build();
            await container.StartAsync();

            return container;
        }

        private static async Task ShutdownDatabaseContainerAsync(TestcontainersContainer container)
        {
            await container.CleanUpAsync();
            await container.DisposeAsync();
        }
    }
}
