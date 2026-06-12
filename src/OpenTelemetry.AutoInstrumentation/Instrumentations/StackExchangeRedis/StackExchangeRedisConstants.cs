// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis;

internal static class StackExchangeRedisConstants
{
    public const string AssemblyName = "StackExchange.Redis";

    public const string MinimumVersion = "2.0.0"; // 2.* packages use assembly version 2.0.0.0
#if NET
    public const string MaximumVersion = "3.65535.65535"; // 3.0.2-preview uses assembly version 3.0.0.0
#else
    public const string MaximumVersion = "2.65535.65535";
#endif
    public const string IntegrationName = "StackExchangeRedis";

    public const string ConnectionMultiplexerTypeName = "StackExchange.Redis.ConnectionMultiplexer";
    public const string ConfigurationOptionsTypeName = "StackExchange.Redis.ConfigurationOptions";
    public const string TextWriterTypeName = "System.IO.TextWriter";
    public const string TaskConnectionMultiplexerTypeName = $"System.Threading.Tasks.Task`1[{ConnectionMultiplexerTypeName}]";
    public const string NullableServerTypeTypeName = $"System.Nullable`1[{ServerTypeTypeName}]";
    public const string EndPointCollectionTypeName = "StackExchange.Redis.EndPointCollection";

    public const string ConnectImplMethodName = "ConnectImpl";
    public const string ConnectImplAsyncMethodName = "ConnectImplAsync";

    private const string ServerTypeTypeName = "StackExchange.Redis.ServerType";
}
