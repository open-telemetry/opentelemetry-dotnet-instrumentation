using System;
using System.Collections.Generic;
using Datadog.Trace.Propagation.B3;
using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Datadog.Trace.ServiceFabric.Propagators
{
    internal class B3ServiceFabricContextPropagator : ServiceFabricContextPropagator
    {
        public override void InjectContext(PropagationContext context, IServiceRemotingRequestMessageHeader messageHeaders)
        {
            messageHeaders.TryAddHeader(B3HttpHeaderNames.B3TraceId, context, ctx => BitConverter.GetBytes(ctx.TraceId));
            messageHeaders.TryAddHeader(B3HttpHeaderNames.B3ParentId, context, ctx => BitConverter.GetBytes(ctx.ParentSpanId));

            var samplingHeader = GetSamplingHeader(context);
            if (samplingHeader != null)
            {
                messageHeaders.TryAddHeader(samplingHeader.Value.Key, context, ctx => BitConverter.GetBytes(samplingHeader.Value.Value));
            }
        }

        public override PropagationContext? ExtractContext(IServiceRemotingRequestMessageHeader messageHeaders)
        {
            ulong traceId = messageHeaders.TryGetHeaderValueUInt64(B3HttpHeaderNames.B3TraceId) ?? 0;

            if (traceId > 0)
            {
                ulong parentSpanId = messageHeaders.TryGetHeaderValueUInt64(B3HttpHeaderNames.B3ParentId) ?? 0;

                if (parentSpanId > 0)
                {
                    SamplingPriority? samplingPriority = ParseB3Sampling(messageHeaders);

                    return new PropagationContext(traceId, parentSpanId, samplingPriority, origin: null);
                }
            }

            return null;
        }

        private static SamplingPriority? ParseB3Sampling(IServiceRemotingRequestMessageHeader messageHeaders)
        {
            var debugged = messageHeaders.TryGetHeaderValueInt32(B3HttpHeaderNames.B3Flags);
            var sampled = messageHeaders.TryGetHeaderValueInt32(B3HttpHeaderNames.B3Sampled);

            if (debugged != null && (debugged == 0 || debugged == 1))
            {
                return debugged == 1 ? SamplingPriority.UserKeep : (SamplingPriority?)null;
            }
            else if (sampled != null && (sampled == 0 || sampled == 1))
            {
                return sampled == 1 ? SamplingPriority.AutoKeep : SamplingPriority.AutoReject;
            }

            return null;
        }

        private static KeyValuePair<string, int>? GetSamplingHeader(PropagationContext context)
        {
            var samplingPriority = (int?)context.SamplingPriority;
            if (samplingPriority != null)
            {
                var value = samplingPriority < (int)SamplingPriority.AutoKeep ? 0 : 1;
                var header = samplingPriority == (int)SamplingPriority.UserKeep ? B3HttpHeaderNames.B3Flags : B3HttpHeaderNames.B3Sampled;

                return new KeyValuePair<string, int>(header, value);
            }

            return null;
        }
    }
}
