// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB;

internal static class MongoDBConstants
{
    public const string IntegrationName = "MongoDB";
    public const string AssemblyName = "MongoDB.Driver";
    public const string TypeName = "MongoDB.Driver.MongoClient";

    public const string MinimumVersion = "2.0.0";
    public const string MaximumVersion = "3.*.*";
}
