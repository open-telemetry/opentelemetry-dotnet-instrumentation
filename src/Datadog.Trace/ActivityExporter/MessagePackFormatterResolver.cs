using System;
using Datadog.Trace;
using Datadog.Trace.Agent.MessagePack;
using Datadog.Trace.Vendors.MessagePack;
using Datadog.Trace.Vendors.MessagePack.Formatters;
using Datadog.Trace.Vendors.MessagePack.Resolvers;
using OpenTelemetry.AutoInstrumentation.ActivityCollector;
using OpenTelemetry.DynamicActivityBinding;

namespace Datadog.AutoInstrumentation.ActivityExporter
{
    internal class MessagePackFormatterResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new MessagePackFormatterResolver();

        private static readonly IMessagePackFormatter<Span> SpanFormatter = new SpanMessagePackFormatter();

        private static readonly IMessagePackFormatter<ActivityStub> ActivityFormatter = new ActivityMessagePackFormatter();

        private static readonly IMessagePackFormatter<LocalTrace> LocalTraceFormatter = new LocalTraceMessagePackFormatter();

        private MessagePackFormatterResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(Span))
            {
                return (IMessagePackFormatter<T>)SpanFormatter;
            }
            else if (typeof(T) == typeof(LocalTrace))
            {
                return (IMessagePackFormatter<T>)LocalTraceFormatter;
            }
            else if (typeof(T) == typeof(ActivityStub))
            {
                return (IMessagePackFormatter<T>)ActivityFormatter;
            }

            return StandardResolver.Instance.GetFormatter<T>();
        }
    }
}
