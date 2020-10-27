using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Vendors.MessagePack;
using Datadog.Trace.Vendors.MessagePack.Formatters;
using OpenTelemetry.AutoInstrumentation.ActivityCollector;
using OpenTelemetry.DynamicActivityBinding;

namespace Datadog.AutoInstrumentation.ActivityExporter
{
    internal class LocalTraceMessagePackFormatter : IMessagePackFormatter<LocalTrace>
    {
        public int Serialize(ref byte[] bytes, int offset, LocalTrace value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Activities.Count);

            foreach (ActivityStub activity in value.Activities)
            {
                offset += ActivityMessagePackFormatter.SerializeActivity(ref bytes, offset, activity, formatterResolver);
            }

            return offset - startOffset;
        }

        public LocalTrace Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            throw new NotImplementedException();
        }
    }
}
