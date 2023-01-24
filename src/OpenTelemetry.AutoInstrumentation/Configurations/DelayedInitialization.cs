// <copyright file="DelayedInitialization.cs" company="OpenTelemetry Authors">
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

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Loading.Initializers;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class DelayedInitialization
{
    internal static class Traces
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddAspNet(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
#if NET462
            new AspNetInitializer(lazyInstrumentationLoader, pluginManager);
#elif NET6_0_OR_GREATER
            lazyInstrumentationLoader.Add(new AspNetCoreInitializer(pluginManager));
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddHttpClient(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            new HttpClientInitializer(lazyInstrumentationLoader, pluginManager);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddGrpcClient(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            lazyInstrumentationLoader.Add(new GrpcClientInitializer(pluginManager));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddSqlClient(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            new SqlClientInitializer(lazyInstrumentationLoader, pluginManager);
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddMySqlClient(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            lazyInstrumentationLoader.Add(new MySqlDataInitializer(pluginManager));
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddWcf(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            lazyInstrumentationLoader.Add(new WcfInitializer(pluginManager));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddQuartz(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            lazyInstrumentationLoader.Add(new QuartzInitializer(pluginManager));
        }
    }

    internal static class Metrics
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddAspNet(LazyInstrumentationLoader lazyInstrumentationLoader)
        {
#if NET462
            new AspNetMetricsInitializer(lazyInstrumentationLoader);
#elif NET6_0_OR_GREATER
            lazyInstrumentationLoader.Add(new AspNetCoreMetricsInitializer());
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddHttpClient(LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            new HttpClientMetricsInitializer(lazyInstrumentationLoader);
        }
    }
}
