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
using OpenTelemetry.Exporter;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Global Settings
/// </summary>
public abstract class Settings
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
        Http2UnencryptedSupportEnabled = source.GetBool(ConfigurationKeys.Http2UnencryptedSupportEnabled) ?? false;
        FlushOnUnhandledException = source.GetBool(ConfigurationKeys.FlushOnUnhandledException) ?? false;
    }

    /// <summary>
    /// Gets the the OTLP transport protocol. Supported values: Grpc and HttpProtobuf.
    /// </summary>
    public OtlpExportProtocol? OtlpExportProtocol { get; }

    /// <summary>
    /// Gets a value indicating whether the `System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport`
    /// should be enabled.
    /// It is required by OTLP gRPC exporter on .NET Core 3.x.
    /// Default is <c>false</c>.
    /// </summary>
    public bool Http2UnencryptedSupportEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="AppDomain.UnhandledException"/> event should trigger
    /// the flushing of telemetry data.
    /// Default is <c>false</c>.
    /// </summary>
    public bool FlushOnUnhandledException { get; }

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
