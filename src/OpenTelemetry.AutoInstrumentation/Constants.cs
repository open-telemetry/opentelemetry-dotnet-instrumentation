// <copyright file="Constants.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation;

internal static class Constants
{
    public static class DistributionAttributes
    {
        public const string TelemetryDistroNameAttributeName = "telemetry.distro.name";
        public const string TelemetryDistroNameAttributeValue = "opentelemetry-dotnet-instrumentation";
        public const string TelemetryDistroVersionAttributeName = "telemetry.distro.version";
    }

    public static class ConfigurationValues
    {
        public const string None = "none";

        /// <summary>
        /// Default delimiter for textual representation of multi-valued settings.
        /// </summary>
        public const char Separator = ',';

        /// <summary>a
        /// Delimiter for textual representation of settings that may contain multiple
        /// fully qualified .NET names, e.g.: assembly or type names, that already
        /// use commas as separators.
        /// </summary>
        public const char DotNetQualifiedNameSeparator = ':';

        public static class Exporters
        {
            public const string Otlp = "otlp";
            public const string Prometheus = "prometheus";
            public const string Zipkin = "zipkin";
        }

        public static class Propagators
        {
            public const string W3CTraceContext = "tracecontext";
            public const string W3CBaggage = "baggage";
            public const string B3Multi = "b3multi";
            public const string B3Single = "b3";
        }

        public static class LogLevel
        {
            public const string Error = "error";
            public const string Warning = "warn";
            public const string Information = "info";
            public const string Debug = "debug";
        }
    }

    public static class EnvironmentVariables
    {
        public const string ProfilerEnabledVariable = "CORECLR_ENABLE_PROFILING";
        public const string ProfilerIdVariable = "CORECLR_PROFILER";
        public const string ProfilerPathVariable = "CORECLR_PROFILER_PATH";
        public const string Profiler32BitPathVariable = "CORECLR_PROFILER_PATH_32";
        public const string Profiler64BitPathVariable = "CORECLR_PROFILER_PATH_64";
        public const string ProfilerId = "{918728DD-259F-4A6A-AC2B-B85E1B658318}";
    }
}
