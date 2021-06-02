using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Tagging;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Util;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.AutoInstrumentation.MongoDb
{
    internal class MongoDbTags : InstrumentationTags
    {
        protected static readonly IProperty<string>[] MongoDbTagsProperties =
            InstrumentationTagsProperties.Concat(
                new ReadOnlyProperty<MongoDbTags, string>(Tags.InstrumentationName, t => t.InstrumentationName),
                new Property<MongoDbTags, string>(Tags.MongoDbName, t => t.DbName, (t, v) => t.DbName = v),
                new Property<MongoDbTags, string>(Tags.MongoDbQuery, t => t.Query, (t, v) => t.Query = v),
                new Property<MongoDbTags, string>(Tags.MongoDbCollection, t => t.Collection, (t, v) => t.Collection = v),
                new Property<MongoDbTags, string>(Tags.OutHost, t => t.Host, (t, v) => t.Host = v),
                new Property<MongoDbTags, string>(Tags.OutPort, t => t.Port, (t, v) => t.Port = v));

        public override ActivityKind Kind => ActivityKind.Client;

        public string InstrumentationName => MongoDbIntegration.IntegrationName;

        public string DbName { get; set; }

        public string Query { get; set; }

        public string Collection { get; set; }

        public string Host { get; set; }

        public string Port { get; set; }

        protected override IProperty<string>[] GetAdditionalTags() => MongoDbTagsProperties;
    }
}
