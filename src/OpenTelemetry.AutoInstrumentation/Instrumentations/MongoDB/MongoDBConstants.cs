// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB;

internal static class MongoDBConstants
{
    public const string IntegrationName = "MongoDB";

    // 3.0.0+
    public const string AssemblyName3 = "MongoDB.Driver";
    public const string MinimumVersion3 = "3.0.0";
    public const string MaximumVersion3 = "3.*.*";

    // 2.7.0+
    public const string AssemblyName = "MongoDB.Driver.Core";
    public const string MinimumVersion = "2.7.0";
    public const string MaximumVersion = "2.*.*";
}
