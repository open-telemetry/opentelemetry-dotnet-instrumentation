// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
        EntityFrameworkCoreSetDbStatementForText = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.EntityFrameworkCoreSetDbStatementForText) ?? false;
#endif

        GraphQLSetDocument = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.GraphQLSetDocument) ?? false;
        GrpcNetClientInstrumentationCaptureRequestMetadata = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.GrpcNetClientInstrumentationCaptureRequestMetadata, AdditionalTag.CreateGrpcRequestCache);
        GrpcNetClientInstrumentationCaptureResponseMetadata = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.GrpcNetClientInstrumentationCaptureResponseMetadata, AdditionalTag.CreateGrpcResponseCache);
        HttpInstrumentationCaptureRequestHeaders = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.HttpInstrumentationCaptureRequestHeaders, AdditionalTag.CreateHttpRequestCache);
        HttpInstrumentationCaptureResponseHeaders = configuration.ParseHeaders(ConfigurationKeys.Traces.InstrumentationOptions.HttpInstrumentationCaptureResponseHeaders, AdditionalTag.CreateHttpResponseCache);
        OracleMdaSetDbStatementForText = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.OracleMdaSetDbStatementForText) ?? false;
        SqlClientSetDbStatementForText = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.SqlClientSetDbStatementForText) ?? false;
    }

#if NETFRAMEWORK
    /// <summary>
    /// Gets the list of HTTP request headers to be captured as the span tags by ASP.NET instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> AspNetInstrumentationCaptureRequestHeaders { get; }

    /// <summary>
    /// Gets the list of HTTP response headers to be captured as the span tags by ASP.NET instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> AspNetInstrumentationCaptureResponseHeaders { get; }
#endif

#if NET
    /// <summary>
    /// Gets the list of HTTP request headers to be captured as the span tags by ASP.NET Core instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> AspNetCoreInstrumentationCaptureRequestHeaders { get; }

    /// <summary>
    /// Gets the list of HTTP response headers to be captured as the span tags by ASP.NET Core instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> AspNetCoreInstrumentationCaptureResponseHeaders { get; }

    /// <summary>
    /// Gets a value indicating whether text query in Entity Framework Core can be passed as a db.statement tag.
    /// </summary>
    public bool EntityFrameworkCoreSetDbStatementForText { get; }
#endif

    /// <summary>
    /// Gets a value indicating whether GraphQL query can be passed as a Document tag.
    /// </summary>
    public bool GraphQLSetDocument { get; }

    /// <summary>
    /// Gets the list of request metadata to be captured as the span tags by Grpc.Net.Client instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> GrpcNetClientInstrumentationCaptureRequestMetadata { get; }

    /// <summary>
    /// Gets the list of response metadata to be captured as the span tags by Grpc.Net.Client instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> GrpcNetClientInstrumentationCaptureResponseMetadata { get; }

    /// <summary>
    /// Gets the list of HTTP request headers to be captured as the span tags by HTTP instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> HttpInstrumentationCaptureRequestHeaders { get; }

    /// <summary>
    /// Gets the list of HTTP response headers to be captured as the span tags by HTTP instrumentation.
    /// </summary>
    public IReadOnlyList<AdditionalTag> HttpInstrumentationCaptureResponseHeaders { get; }

    /// <summary>
    /// Gets a value indicating whether text query in Oracle Client can be passed as a db.statement tag.
    /// </summary>
    public bool OracleMdaSetDbStatementForText { get; }

    /// <summary>
    /// Gets a value indicating whether text query in SQL Client can be passed as a db.statement tag.
    /// </summary>
    public bool SqlClientSetDbStatementForText { get; }
}
