// <copyright file="Plugin.cs" company="OpenTelemetry Authors">
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

using System;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace TestApplication.Plugins;

public class Plugin
{
    public void Initializing()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(Initializing)}() invoked.");
    }

    public TracerProviderBuilder ConfigureTracerProvider(TracerProviderBuilder builder)
    {
        return builder.AddSource(TestApplication.Smoke.Program.SourceName);
    }

    public MeterProviderBuilder ConfigureMeterProvider(MeterProviderBuilder builder)
    {
        return builder.AddMeter(TestApplication.Smoke.Program.SourceName);
    }

    public void ConfigureTracesOptions(HttpClientInstrumentationOptions options)
    {
#if NETFRAMEWORK
        options.EnrichWithHttpWebRequest = (activity, message) =>
#else
        options.EnrichWithHttpRequestMessage = (activity, message) =>
#endif
        {
            activity.SetTag("example.plugin", "MyExamplePlugin");
        };
    }

    public void ConfigureTracesOptions(OtlpExporterOptions options)
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureTracesOptions)}({nameof(OtlpExporterOptions)} {nameof(options)}) invoked.");
    }

    public void ConfigureMetricsOptions(OtlpExporterOptions options)
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureMetricsOptions)}({nameof(OtlpExporterOptions)} {nameof(options)}) invoked.");
    }
}
