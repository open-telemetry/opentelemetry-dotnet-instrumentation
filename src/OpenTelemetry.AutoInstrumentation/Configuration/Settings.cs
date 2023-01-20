// <copyright file="Settings.cs" company="OpenTelemetry Authors">
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

using System.Reflection;
using OpenTelemetry.Exporter;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Global Settings
/// </summary>
internal abstract class Settings
{
    /// <summary>
    /// Gets the the OTLP transport protocol. Supported values: Grpc and HttpProtobuf.
    /// </summary>
    public OtlpExportProtocol? OtlpExportProtocol { get; private set; }

    public static T FromDefaultSources<T>()
        where T : Settings, new()
    {
#if NETFRAMEWORK
        // on .NET Framework only, also read from app.config/web.config
        var configuration = new Configuration(new EnvironmentConfigurationSource(), new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings));
#else
        var configuration = new Configuration(new EnvironmentConfigurationSource());
#endif
        var settings = new T();
        settings.Load(configuration);
        return settings;
    }

    public void Load(Configuration configuration)
    {
        OtlpExportProtocol = GetExporterOtlpProtocol(configuration);
        OnLoad(configuration);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Settings"/> class
    /// using the specified <see cref="Configuration"/> to initialize values.
    /// </summary>
    /// <param name="configuration">The <see cref="Configuration"/> to use when retrieving configuration values.</param>
    protected abstract void OnLoad(Configuration configuration);

    private static OtlpExportProtocol? GetExporterOtlpProtocol(Configuration configuration)
    {
        // the default in SDK is grpc. http/protobuf should be default for our purposes
        var exporterOtlpProtocol = configuration.GetString(ConfigurationKeys.ExporterOtlpProtocol);

        if (string.IsNullOrEmpty(exporterOtlpProtocol))
        {
            // override settings only for http/protobuf
            return Exporter.OtlpExportProtocol.HttpProtobuf;
        }

        // null value here means that it will be handled by OTEL .NET SDK
        return null;
    }
}
