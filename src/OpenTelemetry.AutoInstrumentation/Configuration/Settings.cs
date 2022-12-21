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

using System;
using System.Reflection;
using OpenTelemetry.Exporter;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Global Settings
/// </summary>
internal abstract class Settings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Settings"/> class
    /// using the specified <see cref="IConfigurationSource"/> to initialize values.
    /// </summary>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    protected Settings(IConfigurationSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        OtlpExportProtocol = GetExporterOtlpProtocol(source);
    }

    /// <summary>
    /// Gets the the OTLP transport protocol. Supported values: Grpc and HttpProtobuf.
    /// </summary>
    public OtlpExportProtocol? OtlpExportProtocol { get; }

    public static T FromDefaultSources<T>()
        where T : Settings
    {
        var configurationSource = new CompositeConfigurationSource
        {
            new EnvironmentConfigurationSource(),

#if NETFRAMEWORK
            // on .NET Framework only, also read from app.config/web.config
            new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif
        };

        try
        {
            var ttt = typeof(T);

            return (T)typeof(T)!
                .GetConstructor(new[] { typeof(IConfigurationSource) })!
                .Invoke(new object[] { configurationSource });
        }
        catch (TargetInvocationException ex)
        {
            // Unwrap the more informative internal exception
            throw ex.InnerException ?? ex;
        }
    }

    private static OtlpExportProtocol? GetExporterOtlpProtocol(IConfigurationSource source)
    {
        // the default in SDK is grpc. http/protobuf should be default for our purposes
        var exporterOtlpProtocol = source.GetString(ConfigurationKeys.ExporterOtlpProtocol);

        if (string.IsNullOrEmpty(exporterOtlpProtocol))
        {
            // override settings only for http/protobuf
            return Exporter.OtlpExportProtocol.HttpProtobuf;
        }

        // null value here means that it will be handled by OTEL .NET SDK
        return null;
    }
}
