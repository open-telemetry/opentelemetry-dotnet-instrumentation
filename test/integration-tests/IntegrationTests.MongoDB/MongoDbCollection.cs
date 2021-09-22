using System;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.WaitStrategies;
using IntegrationTests.Helpers;
using IntegrationTests.Helpers.Enums;
using Xunit;

namespace IntegrationTests.MongoDB
{
    [CollectionDefinition(Name)]
    public class MongoDbCollection : ICollectionFixture<MongoDbFixture>
    {
        public const string Name = nameof(MongoDbCollection);
    }

    public class MongoDbFixture : IDisposable
    {
        private const int MongoDbPort = 27017;

        private TestcontainersContainer _container;

        public MongoDbFixture()
        {
            bool launchContainer = ShouldLaunchContainer();

            Port = launchContainer
                ? TcpPortProvider.GetOpenPort()
                : MongoDbPort;

            if (launchContainer)
            {
                _container = LaunchMongoContainer(Port);
            }
        }

        public int Port { get; }

        public void Dispose()
        {
            if (_container != null)
            {
                ShutDownMongoContainer(_container);
            }
        }

        private bool ShouldLaunchContainer()
        {
            var environment = EnvironmentHelper.GetIntegrationsEnvironment();

            if (environment == IntegrationsEnvironment.CI)
            {
                if (EnvironmentTools.IsWindows() ||
                    EnvironmentTools.IsMacOS())
                {
                    return false;
                }
                else if (EnvironmentTools.IsLinux())
                {
                    return true;
                }
            }

            return true;
        }

        private TestcontainersContainer LaunchMongoContainer(int port)
        {
            var waitOS = EnvironmentTools.IsWindows()
                ? Wait.ForWindowsContainer()
                : Wait.ForUnixContainer();

            var mongoContainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
              .WithImage("mongo")
              .WithName($"mongo-db-{port}")
              .WithPortBinding(port, MongoDbPort)
              .WithWaitStrategy(waitOS.UntilPortIsAvailable(MongoDbPort));

            var container = mongoContainersBuilder.Build();
            container.StartAsync().Wait();

            return container;
        }

        private void ShutDownMongoContainer(TestcontainersContainer container)
        {
            container.CleanUpAsync().Wait();
            container.DisposeAsync().AsTask().Wait();
        }
    }
}
