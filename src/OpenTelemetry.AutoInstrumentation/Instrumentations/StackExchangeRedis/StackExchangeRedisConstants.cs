// <copyright file="StackExchangeRedisConstants.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#if NETCOREAPP3_1_OR_GREATER

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis;

internal static class StackExchangeRedisConstants
{
    public const string AssemblyName = "StackExchange.Redis";

    public const string MinimumVersion = "2.0.0"; // this is AssemblyVersion, all 2.* versions are released with this value
    public const string MaximumVersion = "2.65535.65535";
    public const string IntegrationName = "StackExchangeRedis";

    public const string ConnectionMultiplexerTypeName = "StackExchange.Redis.ConnectionMultiplexer";
    public const string ConfigurationOptionsTypeName = "StackExchange.Redis.ConfigurationOptions";
    public const string TextWriterTypeName = "System.IO.TextWriter";
    public const string TaskConnectionMultiplexerTypeName = $"System.Threading.Tasks.Task`1[{ConnectionMultiplexerTypeName}]";
    public const string NullableServerType = $"System.Nullable`1[{ServerTypeTypeName}]";

    public const string ConnectImplMethodName = "ConnectImpl";
    public const string ConnectImplAsyncMethodName = "ConnectImplAsync";

    private const string ServerTypeTypeName = "StackExchange.Redis.ServerType";
}
#endif
