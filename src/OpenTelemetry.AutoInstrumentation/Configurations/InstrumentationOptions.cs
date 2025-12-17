// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.HeadersCapture;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Instrumentation options
/// </summary>
internal class InstrumentationOptions
{
    internal InstrumentationOptions(Configuration configuration)
    {
#if NETFRAMEWORK
        AspNetInstrumentationCaptureRequestHeaders = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.AspNetInstrumentationCaptureRequestHeaders, AdditionalTag.CreateHttpRequestCache);
        AspNetInstrumentationCaptureResponseHeaders = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.AspNetInstrumentationCaptureResponseHeaders, AdditionalTag.CreateHttpResponseCache);
#endif
#if NET
        AspNetCoreInstrumentationCaptureRequestHeaders = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.AspNetCoreInstrumentationCaptureRequestHeaders, AdditionalTag.CreateHttpRequestCache);
        AspNetCoreInstrumentationCaptureResponseHeaders = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.AspNetCoreInstrumentationCaptureResponseHeaders, AdditionalTag.CreateHttpResponseCache);
        GraphQLSetDocument = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.GraphQLSetDocument) ?? false;
#endif

        GrpcNetClientInstrumentationCaptureRequestMetadata = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.GrpcNetClientInstrumentationCaptureRequestMetadata, AdditionalTag.CreateGrpcRequestCache);
        GrpcNetClientInstrumentationCaptureResponseMetadata = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.GrpcNetClientInstrumentationCaptureResponseMetadata, AdditionalTag.CreateGrpcResponseCache);
        HttpInstrumentationCaptureRequestHeaders = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders, AdditionalTag.CreateHttpRequestCache);
        HttpInstrumentationCaptureResponseHeaders = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders, AdditionalTag.CreateHttpResponseCache);
        OracleMdaSetDbStatementForText = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.OracleMdaSetDbStatementForText) ?? false;
        SqlClientNetFxIlRewriteEnabled = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.SqlClientNetFxILRewriteEnabled) ?? false;
    }

    internal InstrumentationOptions(DotNetTraces? instrumentationConfiguration)
    {
        if (instrumentationConfiguration != null)
        {
#if NETFRAMEWORK
            if (instrumentationConfiguration.AspNet != null)
            {
                AspNetInstrumentationCaptureRequestHeaders = HeaderConfigurationExtensions.ParseHeaders(instrumentationConfiguration.AspNet.CaptureRequestHeaders, AdditionalTag.CreateHttpRequestCache);
                AspNetInstrumentationCaptureResponseHeaders = HeaderConfigurationExtensions.ParseHeaders(instrumentationConfiguration.AspNet.CaptureResponseHeaders, AdditionalTag.CreateHttpResponseCache);
            }
#endif
#if NET
            if (instrumentationConfiguration.AspNetCore != null)
            {
                AspNetCoreInstrumentationCaptureRequestHeaders = HeaderConfigurationExtensions.ParseHeaders(instrumentationConfiguration.AspNetCore.CaptureRequestHeaders, AdditionalTag.CreateHttpRequestCache);
                AspNetCoreInstrumentationCaptureResponseHeaders = HeaderConfigurationExtensions.ParseHeaders(instrumentationConfiguration.AspNetCore.CaptureResponseHeaders, AdditionalTag.CreateHttpResponseCache);
            }

            if (instrumentationConfiguration.GraphQL != null)
            {
                GraphQLSetDocument = instrumentationConfiguration.GraphQL.SetDocument;
            }
#endif

            if (instrumentationConfiguration.GrpcNetClient != null)
            {
                GrpcNetClientInstrumentationCaptureRequestMetadata = HeaderConfigurationExtensions.ParseHeaders(instrumentationConfiguration.GrpcNetClient.CaptureRequestMetadata, AdditionalTag.CreateGrpcRequestCache);
                GrpcNetClientInstrumentationCaptureResponseMetadata = HeaderConfigurationExtensions.ParseHeaders(instrumentationConfiguration.GrpcNetClient.CaptureResponseMetadata, AdditionalTag.CreateGrpcResponseCache);
            }

            if (instrumentationConfiguration.HttpClient != null)
            {
                HttpInstrumentationCaptureRequestHeaders = HeaderConfigurationExtensions.ParseHeaders(instrumentationConfiguration.HttpClient.CaptureRequestHeaders, AdditionalTag.CreateHttpRequestCache);
                HttpInstrumentationCaptureResponseHeaders = HeaderConfigurationExtensions.ParseHeaders(instrumentationConfiguration.HttpClient.CaptureResponseHeaders, AdditionalTag.CreateHttpResponseCache);
            }

            if (instrumentationConfiguration.OracleMda != null)
            {
                OracleMdaSetDbStatementForText = instrumentationConfiguration.OracleMda.SetDbStatementForText;
            }

            if (instrumentationConfiguration.SqlClient != null)
            {
                SqlClientNetFxIlRewriteEnabled = instrumentationConfiguration.SqlClient.NetFxIlRewriteEnabled;
            }
        }
    }

#if NETFRAMEWORK
    /// <summary>
    /// Gets the list of HTTP request headers to be captured as the span tags by ASP.NET instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> AspNetInstrumentationCaptureRequestHeaders { get; } = [];

    /// <summary>
    /// Gets the list of HTTP response headers to be captured as the span tags by ASP.NET instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> AspNetInstrumentationCaptureResponseHeaders { get; } = [];
#endif

#if NET
    /// <summary>
    /// Gets the list of HTTP request headers to be captured as the span tags by ASP.NET Core instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> AspNetCoreInstrumentationCaptureRequestHeaders { get; } = [];

    /// <summary>
    /// Gets the list of HTTP response headers to be captured as the span tags by ASP.NET Core instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> AspNetCoreInstrumentationCaptureResponseHeaders { get; } = [];

    /// <summary>
    /// Gets a value indicating whether GraphQL query can be passed as a Document tag.
    /// </summary>
    public bool GraphQLSetDocument { get; }
#endif

    /// <summary>
    /// Gets the list of request metadata to be captured as the span tags by Grpc.Net.Client instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> GrpcNetClientInstrumentationCaptureRequestMetadata { get; } = [];

    /// <summary>
    /// Gets the list of response metadata to be captured as the span tags by Grpc.Net.Client instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> GrpcNetClientInstrumentationCaptureResponseMetadata { get; } = [];

    /// <summary>
    /// Gets the list of HTTP request headers to be captured as the span tags by HTTP instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> HttpInstrumentationCaptureRequestHeaders { get; } = [];

    /// <summary>
    /// Gets the list of HTTP response headers to be captured as the span tags by HTTP instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> HttpInstrumentationCaptureResponseHeaders { get; } = [];

    /// <summary>
    /// Gets a value indicating whether text query in Oracle Client can be passed as a db.statement tag.
    /// </summary>
    public bool OracleMdaSetDbStatementForText { get; }

    /// <summary>
    /// Gets a value indicating whether the SqlClient instrumentation on .NET Framework should rewrite IL
    /// to ensure CommandText is available.
    /// </summary>
    public bool SqlClientNetFxIlRewriteEnabled { get; }
}
