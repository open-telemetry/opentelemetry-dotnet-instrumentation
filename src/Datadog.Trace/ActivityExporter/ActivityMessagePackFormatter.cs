using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Vendors.MessagePack;
using Datadog.Trace.Vendors.MessagePack.Formatters;

using OpenTelemetry.DynamicActivityBinding;

namespace Datadog.AutoInstrumentation.ActivityExporter
{
    internal class ActivityMessagePackFormatter : IMessagePackFormatter<ActivityStub>
    {
        public static int SerializeActivity(ref byte[] bytes, int offset, ActivityStub activity, IFormatterResolver formatterResolver)
        {
            EnrichActivity(activity);

            // First, pack array length (or map length).
            // It should be the number of members of the object to be serialized.
            var len = 8;

            bool hasParent = activity.TryGetParent(out ActivityStub parent);
            if (hasParent)
            {
                len++;
            }

            bool? error = (bool?)activity.GetCustomProperty("Error");
            if (error != null && error.Value)
            {
                len++;
            }

            if (activity.Tags != null)
            {
                len++;
            }

            Dictionary<string, double> metrics = (Dictionary<string, double>)activity.GetCustomProperty("Metrics");
            if (metrics != null)
            {
                len++;
            }

            int originalOffset = offset;

            offset += MessagePackBinary.WriteMapHeader(ref bytes, offset, len);

            offset += MessagePackBinary.WriteString(ref bytes, offset, "trace_id");
            offset += MessagePackBinary.WriteUInt64(ref bytes, offset, Convert.HexStringToUInt63(activity.TraceId, byteCount: 16));

            offset += MessagePackBinary.WriteString(ref bytes, offset, "span_id");
            offset += MessagePackBinary.WriteUInt64(ref bytes, offset, Convert.HexStringToUInt63(activity.SpanId, byteCount: 8));

            offset += MessagePackBinary.WriteString(ref bytes, offset, "name");
            offset += MessagePackBinary.WriteString(ref bytes, offset, activity.OperationName);

            offset += MessagePackBinary.WriteString(ref bytes, offset, "resource");
            offset += MessagePackBinary.WriteString(ref bytes, offset, activity.OperationName);

            offset += MessagePackBinary.WriteString(ref bytes, offset, "service");
            offset += MessagePackBinary.WriteString(ref bytes, offset, (string)activity.GetCustomProperty("ServiceName"));

            offset += MessagePackBinary.WriteString(ref bytes, offset, "type");
            offset += MessagePackBinary.WriteString(ref bytes, offset, (string)activity.GetCustomProperty("Type"));

            offset += MessagePackBinary.WriteString(ref bytes, offset, "start");
            offset += MessagePackBinary.WriteInt64(ref bytes, offset, ((DateTimeOffset)activity.StartTimeUtc).ToUnixTimeNanoseconds());

            offset += MessagePackBinary.WriteString(ref bytes, offset, "duration");
            offset += MessagePackBinary.WriteInt64(ref bytes, offset, activity.Duration.ToNanoseconds());

            if (hasParent)
            {
                offset += MessagePackBinary.WriteString(ref bytes, offset, "parent_id");
                offset += MessagePackBinary.WriteUInt64(ref bytes, offset, Convert.HexStringToUInt63(parent.SpanId, byteCount: 8));
            }

            if (error != null && error.Value)
            {
                offset += MessagePackBinary.WriteString(ref bytes, offset, "error");
                offset += MessagePackBinary.WriteByte(ref bytes, offset, 1);
            }

            if (activity.Tags != null)
            {
                offset += MessagePackBinary.WriteString(ref bytes, offset, "meta");
                offset += MessagePackBinary.WriteMapHeader(ref bytes, offset, activity.Tags.Count());

                foreach (var pair in activity.Tags)
                {
                    offset += MessagePackBinary.WriteString(ref bytes, offset, pair.Key);
                    offset += MessagePackBinary.WriteString(ref bytes, offset, pair.Value);
                }
            }

            if (metrics != null)
            {
                offset += MessagePackBinary.WriteString(ref bytes, offset, "metrics");
                offset += MessagePackBinary.WriteMapHeader(ref bytes, offset, metrics.Count);

                foreach (var pair in metrics)
                {
                    offset += MessagePackBinary.WriteString(ref bytes, offset, pair.Key);
                    offset += MessagePackBinary.WriteDouble(ref bytes, offset, pair.Value);
                }
            }

            return offset - originalOffset;
        }

        public int Serialize(ref byte[] bytes, int offset, ActivityStub activity, IFormatterResolver formatterResolver)
        {
            return ActivityMessagePackFormatter.SerializeActivity(ref bytes, offset, activity, formatterResolver);
        }

        public ActivityStub Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            throw new NotImplementedException();
        }

        private static void EnrichActivity(ActivityStub activity)
        {
            // There is no need to add any of this to the activity at this time. We can add this directly to the serialized format.
            // This is just for the POC this way.
            Tracer tracer = Tracer.Instance;
            activity.AddServiceName(tracer.DefaultServiceName)
                    .AddTags(tracer.Settings.GlobalTags) // Add global tags first so service unification tags added later can override them
                    .AddEnvironment(tracer.Settings.Environment)
                    .AddVersion(tracer.Settings.ServiceVersion)
                    .AddAnalyticsSampleRate(tracer.Settings);
        }
    }
}
