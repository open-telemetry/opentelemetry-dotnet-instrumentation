using System;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.WaitStrategies;
using IntegrationTests.Helpers;
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
            bool hasRunningMongoDb = !TcpPortProvider.IsPortOpen(MongoDbPort);

            Port = hasRunningMongoDb
                ? MongoDbPort
                : TcpPortProvider.GetOpenPort();

            if (!hasRunningMongoDb)
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
