using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Conventions;
using Datadog.Trace.Plugins;
using Datadog.Trace.Propagation;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.Propagators
{
    public class CompositePropagatorsProviderTests
    {
        private const string Propagator1 = "Propagator_1";
        private const string Propagator2 = "Propagator_2";
        private const string Propagator3 = "Propagator_3";

        [Fact]
        public void CompositePropagatorsProvider_GetPropagators()
        {
            var provider = new CompositePropagatorsProvider()
                .RegisterProvider(new ProviderStub(Propagator1))
                .RegisterProvider(new ProviderStub(Propagator3));

            var propagators = provider
                .GetPropagators(new[] { Propagator1, Propagator3 }, null)
                .Cast<PropagatorStub>()
                .ToList();

            Assert.Equal(2, propagators.Count());
            Assert.Equal(Propagator1, propagators[0].Id);
            Assert.Equal(Propagator3, propagators[1].Id);
        }

        [Fact]
        public void CompositePropagatorsProvider_GetPropagators_When_NoProvider()
        {
            var provider = new CompositePropagatorsProvider()
                .RegisterProvider(new ProviderStub(Propagator1));

            Assert.Throws<InvalidOperationException>(() => provider
                .GetPropagators(new[] { Propagator2 }, null)
                .Cast<PropagatorStub>()
                .ToList());
        }

        [Fact]
        public void CompositePropagatorsProvider_GetPropagators_When_RegisteringFromPlugins()
        {
            var mockPlugin1 = Mock.Of<IExtendPropagators>(m => m.GetPropagatorsProvider() == new ProviderStub(Propagator1));
            var mockPlugin2 = Mock.Of<IExtendPropagators>(m => m.GetPropagatorsProvider() == new ProviderStub(Propagator2));
            var plugins = new[] { mockPlugin1, mockPlugin2 };
            var provider = new CompositePropagatorsProvider()
                .RegisterProviderFromPlugins(plugins);

            var propagators = provider.GetPropagators(new[] { Propagator1, Propagator2 }, null)
                .Cast<PropagatorStub>()
                .ToList();

            Assert.Equal(2, propagators.Count());
            Assert.Equal(Propagator1, propagators[0].Id);
            Assert.Equal(Propagator2, propagators[1].Id);
        }

        private class ProviderStub : IPropagatorsProvider
        {
            private readonly string _provides;

            public ProviderStub(string provides)
            {
                _provides = provides;
            }

            public bool CanProvide(string propagatorId, ITraceIdConvention traceIdConvention)
            {
                return _provides == propagatorId;
            }

            public IPropagator GetPropagator(string propagatorId, ITraceIdConvention traceIdConvention)
            {
                return new PropagatorStub(propagatorId);
            }
        }

        private class PropagatorStub : IPropagator
        {
            public PropagatorStub(string id)
            {
                Id = id;
            }

            public string Id { get; }

            public SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
            {
                throw new NotImplementedException();
            }

            public void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter)
            {
                throw new NotImplementedException();
            }
        }
    }
}
