// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

using Google.Protobuf;

namespace OpenTelemetry.OpenTelemetryProtocol;

internal sealed class OtlpExporterHttpContent<T> : HttpContent
    where T : IMessage
{
    private const string MediaContentType = "application/x-protobuf";

    private static readonly MediaTypeHeaderValue s_ProtobufMediaTypeHeader = new(MediaContentType);

    private readonly T _Request;

    public OtlpExporterHttpContent(T exportRequest)
    {
        _Request = exportRequest;
        Headers.ContentType = s_ProtobufMediaTypeHeader;
    }

#if NET
    protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        SerializeToStreamInternal(stream);
    }
#endif

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        SerializeToStreamInternal(stream);
        return Task.CompletedTask;
    }

    protected override bool TryComputeLength(out long length)
    {
        // We can't know the length of the content being pushed to the output stream.
        length = -1;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SerializeToStreamInternal(Stream stream)
    {
        _Request.WriteTo(stream);
    }
}
