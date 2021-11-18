using System;
using DotNet.Testcontainers.Containers.Modules;

namespace IntegrationTests.Helpers.Models
{
    public class Container : IDisposable
    {
        private TestcontainersContainer _container;

        public Container(TestcontainersContainer container)
        {
            _container = container;
        }

        public void Dispose()
        {
            _container
                .DisposeAsync()
                .AsTask()
                .Wait(TimeSpan.FromMinutes(5));
        }
    }
}
