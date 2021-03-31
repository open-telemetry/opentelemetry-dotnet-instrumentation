using System.Collections.Generic;
using System.Linq;
using System.Text;
using Datadog.Trace.Propagation;
using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Datadog.Trace.ServiceFabric
{
    internal static class ServiceFabricContextPropagatorExtensions
    {
        public static void Inject(this IPropagator propagator, SpanContext context, IServiceRemotingRequestMessageHeader headers)
        {
            propagator.Inject(context, headers, SetHeader);
        }

        public static SpanContext Extract(this IPropagator propagator, IServiceRemotingRequestMessageHeader headers)
        {
            return propagator.Extract(headers, GetHeader);
        }

        private static IEnumerable<string> GetHeader(IServiceRemotingRequestMessageHeader carrier, string header)
        {
            string? value = carrier.TryGetHeaderValueString(header);

            if (value == null)
            {
                return Enumerable.Empty<string>();
            }

            return new[] { value };
        }

        private static void SetHeader(IServiceRemotingRequestMessageHeader carrier, string header, string value)
        {
            carrier.TryAddHeader(header, value, Encoding.UTF8.GetBytes);
        }
    }
}
