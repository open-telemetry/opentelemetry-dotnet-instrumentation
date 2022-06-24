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

#if !NETFRAMEWORK
using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
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

        private TestcontainersContainer _container;

        public SqlClientFixture()
        {
            Port = TcpPortProvider.GetOpenPort();
        }

        public string Password { get; } = $"@{Guid.NewGuid().ToString("N")}";

        public int Port { get; }

        public async Task InitializeAsync()
        {
            _container = await LaunchDatabaseContainerAsync();
        }

        public async Task DisposeAsync()
        {
            if (_container != null)
            {
                await ShutdownDatabaseContainerAsync(_container);
            }
        }

        private static async Task ShutdownDatabaseContainerAsync(TestcontainersContainer container)
        {
            await container.CleanUpAsync();
            await container.DisposeAsync();
        }

        private async Task<TestcontainersContainer> LaunchDatabaseContainerAsync()
        {
            var databaseContainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage(DatabaseImage)
                .WithName($"sql-server-{Port}")
                .WithPortBinding(Port, DatabasePort)
                .WithEnvironment("SA_PASSWORD", Password)
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(DatabasePort));

            var container = databaseContainersBuilder.Build();
            await container.StartAsync();

            return container;
        }
    }
}
#endif
