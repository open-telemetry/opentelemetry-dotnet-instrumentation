using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Conventions;
using Datadog.Trace.Logging;
using Datadog.Trace.Plugins;

namespace Datadog.Trace.Propagation
{
    internal class CompositePropagatorsProvider
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<CompositePropagatorsProvider>();

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

        public CompositePropagatorsProvider RegisterProviderFromExtensions(IEnumerable<IOTelExtension> extensions)
        {
            foreach (var extension in extensions)
            {
                if (extension is IPropagatorsProvider propagatorsProvider)
                {
                    _providers.Add(propagatorsProvider);
                }
            }

            return this;
        }

        public IEnumerable<IPropagator> GetPropagators(IEnumerable<string> propagatorIds, ITraceIdConvention traceIdConvention)
        {
            return propagatorIds.Select(type => GetPropagator(type, traceIdConvention)).ToList();
        }

        private IPropagator GetPropagator(string propagatorId, ITraceIdConvention traceIdConvention)
        {
            var propagator = _providers
                .Where(x => x.CanProvide(propagatorId, traceIdConvention))
                .Select(x => x.GetPropagator(propagatorId, traceIdConvention))
                .FirstOrDefault();

            if (propagator == null)
            {
                string msg = $"There is no propagator registered for type '{propagatorId}'.";

                Log.Error(msg);

                throw new InvalidOperationException(msg);
            }

            return propagator;
        }
    }
}
