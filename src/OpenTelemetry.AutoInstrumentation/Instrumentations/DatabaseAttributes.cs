// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations;

internal static class DatabaseAttributes
{
    internal static class Keys
    {
        public const string DbSystem = "db.system.name";
        public const string DbCollectionName = "db.collection.name";
        public const string DbNamespace = "db.namespace";
        public const string DbOperationName = "db.operation.name";
    }

    internal static class Values
    {
        internal static class MongoDB
        {
            public const string MongoDbSystem = "mongodb";
        }
    }
}
