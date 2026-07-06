// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET // Contention monitoring requires EventPipe (.NET Core/5+ only)

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Collector.Profiles.V1Development;
using OpenTelemetry.Proto.Profiles.V1Development;

namespace IntegrationTests;

public class ContinuousProfilerContentionTests : TestHelper
{
    public ContinuousProfilerContentionTests(ITestOutputHelper output)
        : base("ContinuousProfiler.Contention", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void DetectsDeadlockGroup()
    {
        EnableBytecodeInstrumentation();
        using var collector = new MockProfilesCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable(
            "OTEL_DOTNET_AUTO_PLUGINS",
            "TestApplication.ContinuousProfiler.Contention.ContentionPlugin, TestApplication.ContinuousProfiler.Contention, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable(
            "OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES",
            "TestApplication.ContinuousProfiler.Contention");

        // All deadlock assertions in ONE predicate because a single export batch
        // contains all three synthetic frame layers, and AssertExpectations only
        // removes one expectation per batch.
        collector.Expect(
            profileData => profileData.ResourceProfiles.Any(rp =>
                rp.ScopeProfiles.Any(sp =>
                    sp.Profiles.Any(profile =>
                        ContainFrameSubstring(profile, profileData.Dictionary, "Deadlock Group") &&
                        ContainFrameSubstring(profile, profileData.Dictionary, "Deadlocked") &&
                        ContainFrameSubstring(profile, profileData.Dictionary, "waiting for lock held by")))),
            "Expected deadlock synthetic frames: 'Deadlock Group' root, 'Deadlocked' intermediate, and per-thread 'waiting for lock held by' labels");

        var (_, _, processId) = RunTestApplication(new TestSettings { Arguments = "deadlock" });

        collector.ResourceExpector.ExpectStandardResources(processId, "TestApplication.ContinuousProfiler.Contention");
        collector.AssertExpectations();
        collector.ResourceExpector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void DetectsStalledConvoyGroup()
    {
        EnableBytecodeInstrumentation();
        using var collector = new MockProfilesCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable(
            "OTEL_DOTNET_AUTO_PLUGINS",
            "TestApplication.ContinuousProfiler.Contention.ContentionPlugin, TestApplication.ContinuousProfiler.Contention, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable(
            "OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES",
            "TestApplication.ContinuousProfiler.Contention");

        // Single combined predicate: AssertExpectations only removes ONE expectation
        // per collected batch, so all checks for one scenario must be in one call.
        collector.Expect(
            profileData => profileData.ResourceProfiles.Any(rp =>
                rp.ScopeProfiles.Any(sp =>
                    sp.Profiles.Any(profile =>
                        ContainFrameSubstring(profile, profileData.Dictionary, "Stalled Group") &&
                        ContainFrameSubstring(profile, profileData.Dictionary, "Waiting for lock held by") &&
                        ContainFrameSubstring(profile, profileData.Dictionary, "Lock Owner")))),
            "Expected convoy synthetic frames: 'Stalled Group' root, 'Waiting for lock held by' intermediate, and 'Lock Owner' marker");

        var (_, _, processId) = RunTestApplication(new TestSettings { Arguments = "convoy" });

        collector.ResourceExpector.ExpectStandardResources(processId, "TestApplication.ContinuousProfiler.Contention");
        collector.AssertExpectations();
        collector.ResourceExpector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void DeadlockGroupContainsNamedThreadsInRingLabel()
    {
        EnableBytecodeInstrumentation();
        using var collector = new MockProfilesCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable(
            "OTEL_DOTNET_AUTO_PLUGINS",
            "TestApplication.ContinuousProfiler.Contention.ContentionPlugin, TestApplication.ContinuousProfiler.Contention, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable(
            "OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES",
            "TestApplication.ContinuousProfiler.Contention");

        // Verify the ring label in the Deadlock Group root carries named threads.
        // All three service names must appear because the cycle is A->B->C->A.
        collector.Expect(
            profileData => profileData.ResourceProfiles.Any(rp =>
                rp.ScopeProfiles.Any(sp =>
                    sp.Profiles.Any(profile =>
                        ContainFrameSubstring(profile, profileData.Dictionary, "ShippingService") &&
                        ContainFrameSubstring(profile, profileData.Dictionary, "OrderService") &&
                        ContainFrameSubstring(profile, profileData.Dictionary, "PaymentService")))),
            "Expected all three named thread identifiers (ShippingService, OrderService, PaymentService) in contention labels");

        var (_, _, processId) = RunTestApplication(new TestSettings { Arguments = "deadlock" });

        collector.ResourceExpector.ExpectStandardResources(processId, "TestApplication.ContinuousProfiler.Contention");
        collector.AssertExpectations();
        collector.ResourceExpector.AssertExpectations();
    }

    /// <summary>
    /// Checks if any frame name in any sample of the profile contains the given substring.
    /// </summary>
    private static bool ContainFrameSubstring(Profile profile, ProfilesDictionary dictionary, string substring)
    {
        foreach (var sample in profile.Samples)
        {
            var stackIndex = sample.StackIndex;
            if (stackIndex <= 0 || stackIndex >= dictionary.StackTable.Count)
            {
                continue;
            }

            var stack = dictionary.StackTable[stackIndex];
            foreach (var frameName in GetFrameNames(stack, dictionary))
            {
                if (frameName.Contains(substring, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> GetFrameNames(Stack stack, ProfilesDictionary dictionary)
    {
        foreach (var locationIndex in stack.LocationIndices)
        {
            if (locationIndex <= 0 || locationIndex >= dictionary.LocationTable.Count)
            {
                continue;
            }

            var location = dictionary.LocationTable[locationIndex];

            foreach (var line in location.Lines)
            {
                var functionIndex = line.FunctionIndex;
                if (functionIndex <= 0 || functionIndex >= dictionary.FunctionTable.Count)
                {
                    continue;
                }

                var function = dictionary.FunctionTable[functionIndex];
                if (function.NameStrindex >= 0 && function.NameStrindex < dictionary.StringTable.Count)
                {
                    yield return dictionary.StringTable[function.NameStrindex];
                }
            }
        }
    }
}

#endif
