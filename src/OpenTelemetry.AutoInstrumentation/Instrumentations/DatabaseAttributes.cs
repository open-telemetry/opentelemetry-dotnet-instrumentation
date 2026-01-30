// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations;

internal static class DatabaseAttributes
{
    public const string SchemaUrl = "https://opentelemetry.io/schemas/1.39.0";

    internal static class Keys
    {
        public const string DbSystemName = "db.system.name";
        public const string DbCollectionName = "db.collection.name";
        public const string DbNamespace = "db.namespace";
        public const string DbOperationName = "db.operation.name";
        public const string DbOperationBatchSize = "db.operation.batch.size";
    }

    internal static class Values
    {
        internal static class MongoDB
        {
            public const string MongoDbSystem = "mongodb";
        }
    }
}
