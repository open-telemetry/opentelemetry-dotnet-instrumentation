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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented

namespace OpenTelemetry.AutoInstrumentation
{
    public static class Constants
    {
        public static class Tracer
        {
            public const string Language = "dotnet";
            public const string Version = "0.2.0";
        }

        public static class ConfigurationValues
        {
            public const string None = "none";

            /// <summary>
            /// Default delimiter for textual representation of multi-valued settings.
            /// </summary>
            public const char Separator = ',';

            /// <summary>a
            /// Delimiter for textual representation of settings that contains multiple
            /// fully qualified .NET names, e.g.: assembly or type names. Comma is part
            /// of a fully qualified name and can't be used here.
            /// </summary>
            public const char DotNetQualifiedNameSeparator = ':';

            public static class Exporters
            {
                public const string Otlp = "otlp";
                public const string Prometheus = "prometheus";
                public const string Zipkin = "zipkin";
                public const string Jaeger = "jaeger";
            }
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600 // Elements should be documented
