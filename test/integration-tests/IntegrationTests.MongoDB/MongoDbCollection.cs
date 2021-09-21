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
        private TestcontainersContainer _container;

        public MongoDbFixture()
        {
            Port = TcpPortProvider.GetOpenPort();

            var waitOS = EnvironmentTools.IsWindows()
                ? Wait.ForWindowsContainer()
                : Wait.ForUnixContainer();

            var mongoContainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
              .WithImage("mongo")
              .WithName($"mongo-db-{Port}")
              .WithPortBinding(Port, 27017)
              .WithWaitStrategy(waitOS.UntilPortIsAvailable(27017));

            _container = mongoContainersBuilder.Build();
            _container.StartAsync().Wait();
        }

        public int Port { get; }

        public void Dispose()
        {
            _container.CleanUpAsync().Wait();
            _container.DisposeAsync().AsTask().Wait();
        }
    }
}
