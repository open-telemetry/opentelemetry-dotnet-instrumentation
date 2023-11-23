// <copyright file="OpenTracingHelper.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Shims.OpenTracing;
using OpenTelemetry.Trace;
using OpenTracing.Util;

namespace OpenTelemetry.AutoInstrumentation;

internal static class OpenTracingHelper
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    public static TracerProviderBuilder AddOpenTracingShimSource(this TracerProviderBuilder tracerProviderBuilder)
    {
        return tracerProviderBuilder.AddSource("opentracing-shim");
    }

    public static void EnableOpenTracing(TracerProvider? tracerProvider)
    {
        try
        {
            if (tracerProvider is not null)
            {
                // Instantiate the OpenTracing shim. The underlying OpenTelemetry tracer will create
                // spans using the opentracing-shim source.
                var openTracingShim = new TracerShim(tracerProvider);

                // This registration must occur prior to any reference to the OpenTracing tracer:
                // otherwise the no-op tracer is going to be used by OpenTracing instead.
                if (GlobalTracer.RegisterIfAbsent(openTracingShim))
                {
                    Logger.Information("OpenTracingShim registered as the OpenTracing global tracer.");
                }
                else
                {
                    Logger.Error(
                        "OpenTracingShim could not be registered as the OpenTracing global tracer." +
                        "Another tracer was already registered. OpenTracing signals will not be captured.");
                }
            }
            else
            {
                Logger.Information("OpenTracingShim was not loaded as the provider is not initialized.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenTracingShim exception.");
            throw;
        }
    }
}
