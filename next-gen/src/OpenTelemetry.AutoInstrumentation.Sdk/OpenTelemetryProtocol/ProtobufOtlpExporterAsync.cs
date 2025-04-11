// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.OpenTelemetryProtocol.Serializer;

namespace OpenTelemetry.OpenTelemetryProtocol;

/// <summary>
/// OTLP exporter base class.
/// </summary>
/// <typeparam name="TBatchWriter"><see cref="IBatchWriter"/> type.</typeparam>
internal abstract class ProtobufOtlpExporterAsync<TBatchWriter> : IExporterAsync<TBatchWriter>
    where TBatchWriter : IBatchWriter
{
    private readonly ILogger _Logger;
    private readonly Uri _RequestUri;
    private readonly HttpClient _HttpClient;
    private readonly IReadOnlyCollection<KeyValuePair<string, string>>? _HeaderOptions;
    private bool _Disposed;

    internal ProtobufOtlpExporterAsync(
        ILogger logger,
        OtlpExporterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ProtocolType != OtlpExporterProtocolType.HttpProtobuf)
        {
            throw new NotSupportedException($"OtlpExporterProtocolType '{options.ProtocolType}' is not supported");
        }

        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _RequestUri = options.Url ?? throw new ArgumentException("Uri was not specified on optons", nameof(options));
        _HeaderOptions = options.Headers;

        _HttpClient = new();
    }

    /// <summary>
    /// Export a telemetry batch.
    /// </summary>
    /// <typeparam name="TBatch"><see cref="IBatch{TBatchWriter}"/> type.</typeparam>
    /// <param name="batch">Batch to export.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> if the export was successful.</returns>
    public abstract Task<bool> ExportAsync<TBatch>(
        in TBatch batch,
        CancellationToken cancellationToken)
        where TBatch : IBatch<TBatchWriter>, allows ref struct;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public HttpRequestMessage CreateHttpRequest(byte[] buffer, int contentLength)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _RequestUri)
        {
            Version = new(2, 0)
        };

        // request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        if (_HeaderOptions != null)
        {
            foreach (var header in _HeaderOptions)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        // TODO: Support compression.
        request.Content = new ByteArrayContent(buffer, 0, contentLength);
        request.Content.Headers.ContentType = new("application/x-protobuf");

        return request;
    }

    /// <summary>
    /// Send an OTLP export request.
    /// </summary>
    /// <param name="writer">Request.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> if the send was successful.</returns>
    protected async Task<bool> SendOtlpAsync(IProtobufWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            using var requestMessage = CreateHttpRequest(writer.Buffer, writer.WritePosition);

            using var responseMessage =
                await _HttpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

            if (!responseMessage.IsSuccessStatusCode)
            {
                _Logger.OtlpTelemetryExportFailed((int)responseMessage.StatusCode, _RequestUri);
                return false;
            }

            _Logger.OtlpTelemetryExportCompleted(_RequestUri);
            return true;
        }
        catch (Exception ex)
        {
            _Logger.OtlpTelemetryExportException(ex, _RequestUri);
            return false;
        }
        finally
        {
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by this class and optionally
    /// releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources;
    /// <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_Disposed)
        {
            if (disposing)
            {
                _HttpClient.Dispose();
            }

            _Disposed = true;
        }
    }
}
