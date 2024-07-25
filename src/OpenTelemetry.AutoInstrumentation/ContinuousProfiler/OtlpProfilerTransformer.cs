// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using System.Diagnostics;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Profiles.V1Experimental;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Profiles.V1Experimental;
using OpenTelemetry.Proto.Resource.V1;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal sealed class OtlpProfilerTransformer
{
    public ExportProfilesServiceRequest? BuildExportRequest(Resource processResource, in Batch<Tuple<byte[], int, string>> profilerBatch)
    {
        // Expected only one element in batch
        foreach (var batchElement in profilerBatch)
        {
            var (buffer, read, type) = batchElement;

            switch (type)
            {
                case "cpu":
                    return ExportThreadSamples(buffer, read);
                case "memory":
                    return ExportAllocationSamples(buffer, read);
            }
        }

        return null;
    }

    public ExportProfilesServiceRequest? ExportThreadSamples(byte[] buffer, int read)
    {
        var threadSamples = SampleNativeFormatParser.ParseThreadSamples(buffer, read);

        if (threadSamples == null || threadSamples.Count == 0)
        {
            return null;
        }

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

        return CreateExportProfilesServiceRequest(profilesData);
    }

    public ExportProfilesServiceRequest? ExportAllocationSamples(byte[] buffer, int read)
    {
        var allocationSamples = SampleNativeFormatParser.ParseAllocationSamples(buffer, read);
        if (allocationSamples.Count == 0)
        {
            return null;
        }

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

        return CreateExportProfilesServiceRequest(profilesData);
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
        resourceProfiles.Resource.Attributes.Add(new KeyValue
        {
            Key = "todo.resource.detector.key", Value = new AnyValue { StringValue = "todo.resource.detector.value" }
        });
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
            ProfileId =
                UnsafeByteOperations.UnsafeWrap(profileByteId), // ProfileId should be same as TraceId - 16 bytes
            StartTimeUnixNano = timestampNanoseconds,
            EndTimeUnixNano = timestampNanoseconds
        };

        profileContainer.Attributes.Add(new KeyValue
        {
            Key = "todo.profiling.data.type", Value = new AnyValue { StringValue = profilingDataType }
        });

        return profileContainer;
    }
}
#endif
