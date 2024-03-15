// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Profiles.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Profiles.V1;
using OpenTelemetry.Proto.Profiles.V1.Alternatives.PprofExtended;
using OpenTelemetry.Proto.Resource.V1;

namespace TestApplication.ContinuousProfiler;

public class OtlpOverHttpExporter
{
    private const string MediaContentType = "application/x-protobuf";

    private readonly string _endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") + "/v1/profiles";
    private readonly HttpClient _httpClient = new();

    public void ExportThreadSamples(byte[] buffer, int read)
    {
        var threadSamples = SampleNativeFormatParser.ParseThreadSamples(buffer, read);

        if (threadSamples == null || threadSamples.Count == 0)
        {
            return;
        }

        try
        {
            var extendedPprofBuilder = new ExtendedPprofBuilder();

            for (var i = 0; i < threadSamples.Count; i++)
            {
                var threadSample = threadSamples[i];

                var sampleBuilder = CreateSampleBuilder(threadSample, extendedPprofBuilder);

                extendedPprofBuilder.Profile.Sample.Add(sampleBuilder.Build());
            }

            var timestampNanoseconds = threadSamples[0].TimestampNanoseconds; // all items in the batch have same timestamp

            var profileContainer = CreateProfileContainer(extendedPprofBuilder.Profile, "cpu", timestampNanoseconds);

            var scopeProfiles = CreateScopeProfiles();
            scopeProfiles.Profiles.Add(profileContainer);

            var resourceProfiles = CreateResourceProfiles(scopeProfiles);

            var profilesData = CreateProfilesData(resourceProfiles);

            var request = CreateExportProfilesServiceRequest(profilesData);

            using var httpRequest = CreateHttpRequest(request);

            using var httpResponse = SendHttpRequest(httpRequest, CancellationToken.None);

            httpResponse.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e);
        }
    }

    public void ExportAllocationSamples(byte[] buffer, int read)
    {
        var allocationSamples = SampleNativeFormatParser.ParseAllocationSamples(buffer, read);
        if (allocationSamples.Count == 0)
        {
            return;
        }

        try
        {
            var scopeProfiles = CreateScopeProfiles();

            var lastTimestamp = allocationSamples[0].ThreadSample.TimestampNanoseconds;
            var extendedPprofBuilder = new ExtendedPprofBuilder();
            var profileContainer = CreateProfileContainer(extendedPprofBuilder.Profile, "allocation", lastTimestamp);
            scopeProfiles.Profiles.Add(profileContainer);

            for (var i = 0; i < allocationSamples.Count; i++)
            {
                var allocationSample = allocationSamples[i];
                if (allocationSample.ThreadSample.TimestampNanoseconds != lastTimestamp)
                {
                    // TODO consider either putting each sample in separate profile or in one profile with min and max timestamp
                    extendedPprofBuilder = new ExtendedPprofBuilder();
                    lastTimestamp = allocationSample.ThreadSample.TimestampNanoseconds;
                    profileContainer = CreateProfileContainer(extendedPprofBuilder.Profile, "allocation", lastTimestamp);
                    scopeProfiles.Profiles.Add(profileContainer);
                }

                var sampleBuilder = CreateSampleBuilder(allocationSample.ThreadSample, extendedPprofBuilder);

                sampleBuilder.SetValue(allocationSample.AllocationSizeBytes);
                extendedPprofBuilder.Profile.Sample.Add(sampleBuilder.Build());
            }

            var resourceProfiles = CreateResourceProfiles(scopeProfiles);

            var profilesData = CreateProfilesData(resourceProfiles);

            var request = CreateExportProfilesServiceRequest(profilesData);

            using var httpRequest = CreateHttpRequest(request);

            using var httpResponse = SendHttpRequest(httpRequest, CancellationToken.None);

            httpResponse.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static SampleBuilder CreateSampleBuilder(ThreadSample threadSample, ExtendedPprofBuilder extendedPprofBuilder)
    {
        var sampleBuilder = new SampleBuilder();

        if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
        {
            extendedPprofBuilder.AddLink(sampleBuilder, threadSample.SpanId, threadSample.TraceIdHigh, threadSample.TraceIdLow);
        }

        for (var index = 0; index < threadSample.Frames.Count; index++)
        {
            var methodName = threadSample.Frames[index];
            sampleBuilder.AddLocationId(extendedPprofBuilder.GetLocationId(methodName));
        }

        if (!string.IsNullOrEmpty(threadSample.ThreadName))
        {
            extendedPprofBuilder.AddAttribute(sampleBuilder, "thread.name", threadSample.ThreadName);
        }

        return sampleBuilder;
    }

    private static ExportProfilesServiceRequest CreateExportProfilesServiceRequest(ProfilesData profilesData)
    {
        var request = new ExportProfilesServiceRequest();

        request.ProfilesData.Add(profilesData);

        return request;
    }

    private static ProfilesData CreateProfilesData(ResourceProfiles resourceProfiles)
    {
        var profilesData = new ProfilesData();

        profilesData.ResourceProfiles.Add(resourceProfiles);

        return profilesData;
    }

    private static ResourceProfiles CreateResourceProfiles(ScopeProfiles scopeProfiles)
    {
        var resourceProfiles = new ResourceProfiles();

        resourceProfiles.ScopeProfiles.Add(scopeProfiles);
        resourceProfiles.Resource = new Resource();
        resourceProfiles.Resource.Attributes.Add(new KeyValue { Key = "todo.resource.detector.key", Value = new AnyValue { StringValue = "todo.resource.detector.value" } });
        // TODO handle schema Url resourceProfiles.SchemaUrl

        return resourceProfiles;
    }

    private static ScopeProfiles CreateScopeProfiles()
    {
        var scopeProfiles = new ScopeProfiles();
        // TODO handle schema Url scopeProfiles.SchemaUrl

        return scopeProfiles;
    }

    private ProfileContainer CreateProfileContainer(Profile profile, string profilingDataType, ulong timestampNanoseconds)
    {
        var profileByteId = new byte[16];
        ActivityTraceId.CreateRandom().CopyTo(profileByteId);

        var profileContainer = new ProfileContainer
        {
            Profile = profile,
            ProfileId = UnsafeByteOperations.UnsafeWrap(profileByteId), // ProfileId should be same as TraceId - 16 bytes
            StartTimeUnixNano = timestampNanoseconds,
            EndTimeUnixNano = timestampNanoseconds
        };

        profileContainer.Attributes.Add(new KeyValue { Key = "todo.profiling.data.type", Value = new AnyValue { StringValue = profilingDataType } });

        return profileContainer;
    }

    private HttpResponseMessage SendHttpRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _httpClient.Send(request, cancellationToken);
    }

    private HttpContent CreateHttpContent(ExportProfilesServiceRequest exportRequest)
    {
        return new ExportRequestContent(exportRequest);
    }

    private HttpRequestMessage CreateHttpRequest(ExportProfilesServiceRequest exportRequest)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);

        // TODO handle OTEL_EXPORTER_OTLP_HEADERS
        // foreach (var header in this.Headers)
        // {
        //     request.Headers.Add(header.Key, header.Value);
        // }

        request.Content = CreateHttpContent(exportRequest);

        return request;
    }

    private sealed class ExportRequestContent : HttpContent
    {
        private static readonly MediaTypeHeaderValue ProtobufMediaTypeHeader = new(MediaContentType);

        private readonly ExportProfilesServiceRequest _exportRequest;

        public ExportRequestContent(ExportProfilesServiceRequest exportRequest)
        {
            _exportRequest = exportRequest;
            Headers.ContentType = ProtobufMediaTypeHeader;
        }

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            SerializeToStreamInternal(stream);
        }

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
            _exportRequest.WriteTo(stream);
        }
    }
}
