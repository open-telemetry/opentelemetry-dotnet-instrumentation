// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Loading.Initializers;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class DelayedInitialization
{
    // Registration may immediately initialize instrumentation for assemblies that are already loaded.
    // Always construct initializers fully before registering them with the loader.
    internal static class Traces
    {
#if NETFRAMEWORK
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddAspNet(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
        {
            new AspNetInitializer(pluginManager, tracerSettings).Register(lazyInstrumentationLoader);
        }
#endif

#if NET
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddAspNetCore(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
        {
            new AspNetCoreInitializer(pluginManager, tracerSettings).Register(lazyInstrumentationLoader);
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddHttpClient(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
        {
            new HttpClientInitializer(pluginManager, tracerSettings).Register(lazyInstrumentationLoader);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddGrpcClient(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
        {
            new GrpcClientInitializer(pluginManager, tracerSettings).Register(lazyInstrumentationLoader);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddSqlClient(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            new SqlClientTracerInitializer(pluginManager).Register(lazyInstrumentationLoader);
        }

#if NET

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddEntityFrameworkCore(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
        {
            new EntityFrameworkCoreInitializer(pluginManager, tracerSettings).Register(lazyInstrumentationLoader);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddGraphQL(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
        {
            new GraphQLInitializer(pluginManager, tracerSettings).Register(lazyInstrumentationLoader);
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddQuartz(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            new QuartzInitializer(pluginManager).Register(lazyInstrumentationLoader);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddOracleMda(LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            new OracleMdaInitializer(tracerSettings).Register(lazyInstrumentationLoader);
        }
    }

    internal static class Metrics
    {
#if NETFRAMEWORK
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddAspNet(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            new AspNetMetricsInitializer(pluginManager).Register(lazyInstrumentationLoader);
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddHttpClient(LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            new HttpClientMetricsInitializer().Register(lazyInstrumentationLoader);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddSqlClient(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            new SqlClientMetricsInitializer(pluginManager).Register(lazyInstrumentationLoader);
        }
    }
}
