using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Conventions;
using Datadog.Trace.Plugins;

namespace Datadog.Trace.Propagation
{
    internal class CompositePropagatorsProvider : IPropagatorsProvider
    {
        private readonly ICollection<IPropagatorsProvider> _providers;

        public CompositePropagatorsProvider()
        {
            _providers = new List<IPropagatorsProvider>();
        }

        public CompositePropagatorsProvider RegisterProvider(IPropagatorsProvider provider)
        {
            _providers.Add(provider);

            return this;
        }

        public CompositePropagatorsProvider RegisterProviderFromPlugins(IEnumerable<IOTelPlugin> plugins)
        {
            foreach (var plugin in plugins)
            {
                _providers.Add(plugin.GetPropagatorsProvider());
            }

            return this;
        }

        public IEnumerable<IPropagator> GetPropagators(IEnumerable<string> propagatorIds, ITraceIdConvention traceIdConvention)
        {
            return propagatorIds.Select(type => GetPropagator(type, traceIdConvention)).ToList();
        }

        public bool CanProvide(string propagatorId, ITraceIdConvention traceIdConvention)
        {
            return _providers.Any(p => p.CanProvide(propagatorId, traceIdConvention));
        }

        public IPropagator GetPropagator(string propagatorId, ITraceIdConvention traceIdConvention)
        {
            var propagator = _providers
                .Where(x => x.CanProvide(propagatorId, traceIdConvention))
                .Select(x => x.GetPropagator(propagatorId, traceIdConvention))
                .FirstOrDefault();

            if (propagator == null)
            {
                throw new InvalidOperationException($"There is no propagator registered for type '{propagatorId}'.");
            }

            return propagator;
        }
    }
}
