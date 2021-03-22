using System;
using System.Text;
using Datadog.Trace.Propagation.Datadog;
using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Datadog.Trace.ServiceFabric.Propagators
{
    internal class DDServiceFabricContextPropagator : ServiceFabricContextPropagator
    {
        public override void InjectContext(PropagationContext context, IServiceRemotingRequestMessageHeader messageHeaders)
        {
            messageHeaders.TryAddHeader(DDHttpHeaderNames.TraceId, context, ctx => BitConverter.GetBytes(ctx.TraceId));
            messageHeaders.TryAddHeader(DDHttpHeaderNames.ParentId, context, ctx => BitConverter.GetBytes(ctx.ParentSpanId));

            if (context.SamplingPriority != null)
            {
                messageHeaders.TryAddHeader(DDHttpHeaderNames.SamplingPriority, context, ctx => BitConverter.GetBytes((int)ctx.SamplingPriority!));
            }

            if (!string.IsNullOrEmpty(context.Origin))
            {
                messageHeaders.TryAddHeader(DDHttpHeaderNames.Origin, context, ctx => Encoding.UTF8.GetBytes(ctx.Origin!));
            }
        }

        public override PropagationContext? ExtractContext(IServiceRemotingRequestMessageHeader messageHeaders)
        {
            ulong traceId = messageHeaders.TryGetHeaderValueUInt64(DDHttpHeaderNames.TraceId) ?? 0;

            if (traceId > 0)
            {
                ulong parentSpanId = messageHeaders.TryGetHeaderValueUInt64(DDHttpHeaderNames.ParentId) ?? 0;

                if (parentSpanId > 0)
                {
                    SamplingPriority? samplingPriority = (SamplingPriority?)messageHeaders.TryGetHeaderValueInt32(DDHttpHeaderNames.SamplingPriority);
                    string? origin = messageHeaders.TryGetHeaderValueString(DDHttpHeaderNames.Origin);

                    return new PropagationContext(traceId, parentSpanId, samplingPriority, origin);
                }
            }

            return null;
        }
    }
}
