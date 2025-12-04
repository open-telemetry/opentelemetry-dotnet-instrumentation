// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Profiles.V1Development;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Profiles.V1Development;
using OpenTelemetry.Proto.Resource.V1;

namespace TestApplication.ContinuousProfiler;

public class OtlpOverHttpExporter
{
    private const string MediaContentType = "application/x-protobuf";

#if NETFRAMEWORK
    private readonly string _endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") + "/v1/metrics";
#else
    private readonly string _endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") + "/v1development/profiles";
#endif
    private readonly HttpClient _httpClient = new();
    private readonly long cpuPeriod;

    private readonly SampleNativeFormatParser _parser;

    public OtlpOverHttpExporter(TimeSpan cpuPeriod, SampleNativeFormatParser parser)
    {
        _parser = parser;
#if NET
        this.cpuPeriod = (long)cpuPeriod.TotalNanoseconds;
#else
        this.cpuPeriod = cpuPeriod.Ticks * 100L; // convert to nanoseconds
#endif
    }

    public void ExportThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var threadSamples = _parser.ParseThreadSamples(buffer, read);

        if (threadSamples == null || threadSamples.Count == 0)
        {
            return;
        }

        try
        {
            var timestampNanoseconds = threadSamples[0].TimestampNanoseconds; // all items in the batch have same timestamp
            var extendedPprofBuilder = new ExtendedPprofBuilder("samples", "count", "cpu", "nanoseconds", cpuPeriod, timestampNanoseconds);

            for (var i = 0; i < threadSamples.Count; i++)
            {
                var threadSample = threadSamples[i];

                var sampleBuilder = CreateSampleBuilder(threadSample, extendedPprofBuilder);
                sampleBuilder.SetValue(1);

                extendedPprofBuilder.Profile.Sample.Add(sampleBuilder.Build());
            }

            var scopeProfiles = CreateScopeProfiles();
            scopeProfiles.Profiles.Add(extendedPprofBuilder.Profile);

            var resourceProfiles = CreateResourceProfiles(scopeProfiles);

            var profilesData = CreateProfilesData(resourceProfiles);

            var request = CreateExportProfilesServiceRequest(profilesData);

            using var httpRequest = CreateHttpRequest(request);

            using var httpResponse = SendHttpRequest(httpRequest, cancellationToken);

            httpResponse.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e);
        }
    }

    public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken)
    {
        var allocationSamples = _parser.ParseAllocationSamples(buffer, read);
        if (allocationSamples.Count == 0)
        {
            return;
        }

        try
        {
            var scopeProfiles = CreateScopeProfiles();

            var lastTimestamp = allocationSamples[0].ThreadSample.TimestampNanoseconds;
            var extendedPprofBuilder = new ExtendedPprofBuilder("allocations", "bytes", null, null, null, lastTimestamp);
            scopeProfiles.Profiles.Add(extendedPprofBuilder.Profile);

            for (var i = 0; i < allocationSamples.Count; i++)
            {
                var allocationSample = allocationSamples[i];
                if (allocationSample.ThreadSample.TimestampNanoseconds != lastTimestamp)
                {
                    // TODO consider either putting each sample in separate profile or in one profile with min and max timestamp
                    lastTimestamp = allocationSample.ThreadSample.TimestampNanoseconds;
                    extendedPprofBuilder = new ExtendedPprofBuilder("allocations", "bytes", null, null, null, lastTimestamp);
                    scopeProfiles.Profiles.Add(extendedPprofBuilder.Profile);
                }

                var sampleBuilder = CreateSampleBuilder(allocationSample.ThreadSample, extendedPprofBuilder);

                sampleBuilder.SetValue(allocationSample.AllocationSizeBytes);
                extendedPprofBuilder.Profile.Sample.Add(sampleBuilder.Build());
            }

            var resourceProfiles = CreateResourceProfiles(scopeProfiles);

            var profilesData = CreateProfilesData(resourceProfiles);

            var request = CreateExportProfilesServiceRequest(profilesData);

            using var httpRequest = CreateHttpRequest(request);

            using var httpResponse = SendHttpRequest(httpRequest, cancellationToken);

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
            var locationId = extendedPprofBuilder.AddLocationId(methodName);

            if (index == 0)
            {
                sampleBuilder.SetLocationRange(locationId, threadSample.Frames.Count);
            }
        }

        if (!string.IsNullOrEmpty(threadSample.ThreadName))
        {
            extendedPprofBuilder.AddAttribute(sampleBuilder, "thread.name", threadSample.ThreadName!);
        }

        return sampleBuilder;
    }

    private static ExportProfilesServiceRequest CreateExportProfilesServiceRequest(ProfilesData profilesData)
    {
        var request = new ExportProfilesServiceRequest();

        request.ResourceProfiles.Add(profilesData.ResourceProfiles);

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

        foreach (var resourceAttribute in ResourcesProvider.Resource.Attributes)
        {
            var value = new AnyValue();
            // TODO better serialization for resources
            switch (resourceAttribute.Value)
            {
                case char c:
                    value.StringValue = c.ToString();
                    break;
                case string s:
                    value.StringValue = s;
                    break;
                case bool b:
                    value.BoolValue = b;
                    break;
                case byte:
                case sbyte:
                case short:
                case ushort:
                case int:
                case uint:
                case long:
                    value.IntValue = (long)resourceAttribute.Value;
                    break;
                case float:
                case double:
                    value.DoubleValue = (double)resourceAttribute.Value;
                    break;
                case Array:
                    // TODO handle arrays before going to production
                    throw new NotImplementedException("Arrays as resources are not implemented.");

                // All other types are converted to strings
                default:
                    try
                    {
                        var stringValue = Convert.ToString(resourceAttribute.Value, CultureInfo.InvariantCulture);
                        value.StringValue = stringValue;
                    }
                    catch
                    {
                        // If ToString throws an exception then the tag is ignored.
                        throw new NotSupportedException("Not supported type for " + resourceAttribute.Value);
                    }

                    break;
            }

            resourceProfiles.Resource.Attributes.Add(new KeyValue
            {
                Key = resourceAttribute.Key,
                Value = value
            });
        }

        // TODO handle schema Url resourceProfiles.SchemaUrl

        return resourceProfiles;
    }

    private static ScopeProfiles CreateScopeProfiles()
    {
        var scopeProfiles = new ScopeProfiles();
        scopeProfiles.Scope = new InstrumentationScope
        {
            Name = "OpenTelemetry.AutoInstrumentation",
            // TODO consider setting Version here
        };

        // TODO handle schema Url scopeProfiles.SchemaUrl

        return scopeProfiles;
    }

    private HttpResponseMessage SendHttpRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#if NET
        return _httpClient.Send(request, cancellationToken);
#else
        return _httpClient.SendAsync(request, cancellationToken).GetAwaiter().GetResult();
#endif
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
            _exportRequest.WriteTo(stream);
        }
    }
}
