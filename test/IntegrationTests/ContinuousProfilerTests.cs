// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Collector.Profiles.V1Development;
using OpenTelemetry.Proto.Profiles.V1Development;
using Xunit.Abstractions;

namespace IntegrationTests;

public class ContinuousProfilerTests : TestHelper
{
    public ContinuousProfilerTests(ITestOutputHelper output)
        : base("ContinuousProfiler", output)
    {
    }

#if NET // allocator tests are only supported on .NET
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ExportAllocationSamples()
    {
        EnableBytecodeInstrumentation();
        using var collector = new MockProfilesCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.ContinuousProfiler.AllocationPlugin, TestApplication.ContinuousProfiler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler");
        var (_, _, processId) = RunTestApplication();

        collector.Expect(profileData => profileData.ResourceProfiles.Any(resourceProfiles => resourceProfiles.ScopeProfiles.Any(scopeProfile => scopeProfile.Profiles.Any(profile => ContainSampleType(profile, profileData.Dictionary, "allocations", "bytes") && profile.Samples[0].Values[0] != 0.0))));
        collector.ResourceExpector.ExpectStandardResources(processId, "TestApplication.ContinuousProfiler");

        collector.AssertExpectations();
        collector.ResourceExpector.AssertExpectations();
    }
#endif

#if NETFRAMEWORK
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ExportAllocationSamples_NetFramework_NoSamplesCollected()
    {
        EnableBytecodeInstrumentation();
        using var collector = new MockProfilesCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.ContinuousProfiler.AllocationPlugin, TestApplication.ContinuousProfiler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler");
        var (_, _, processId) = RunTestApplication();

        collector.AssertEmpty();
    }
#endif

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ExportThreadSamples()
    {
        EnableBytecodeInstrumentation();
        using var collector = new MockProfilesCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.ContinuousProfiler.ThreadPlugin, TestApplication.ContinuousProfiler, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler");
        var expectedStackTrace = string.Join("\n", CreateExpectedStackTrace());

        collector.ExpectCollected(ExpectCollected, "Expect Collected failed");
        collector.Expect(profileData => profileData.ResourceProfiles.Any(resourceProfiles => resourceProfiles.ScopeProfiles.Any(scopeProfile => scopeProfile.Profiles.Any(profile => ContainStackTraceForClassHierarchy(profile, profileData.Dictionary, expectedStackTrace) && ContainSampleType(profile, profileData.Dictionary, "samples", "count") && ContainPeriod(profile, profileData.Dictionary, "cpu", "nanoseconds", 1_000_000_000) && profile.Samples[0].Values[0] == 1))));

        var (_, _, processId) = RunTestApplication();

        collector.ResourceExpector.ExpectStandardResources(processId, "TestApplication.ContinuousProfiler");

        collector.AssertCollected();
        collector.AssertExpectations();
        collector.ResourceExpector.AssertExpectations();
    }

    private static bool ExpectCollected(ICollection<ExportProfilesServiceRequest> c)
    {
        foreach (var request in c)
        {
            foreach (var resourceProfile in request.ResourceProfiles)
            {
                foreach (var scopeProfile in resourceProfile.ScopeProfiles)
                {
                    Assert.Equal("OpenTelemetry.AutoInstrumentation", scopeProfile.Scope.Name);
                }
            }

            var dictionary = request.Dictionary;
            var locationTable = dictionary.LocationTable;

            // skip zero-value entry at index 0
            for (var i = 1; i < locationTable.Count; i++)
            {
                var location = locationTable[i];

                foreach (var index in location.AttributeIndices)
                {
                    var attr = dictionary.AttributeTable[index];
                    if (attr == null || dictionary.StringTable[attr.KeyStrindex] != "profile.frame.type")
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool ContainSampleType(Profile profile, ProfilesDictionary dictionary, string profilingSampleType, string profilingSampleUnit)
    {
        var vt = profile.SampleType;
        return dictionary.StringTable[vt.TypeStrindex] == profilingSampleType &&
            dictionary.StringTable[vt.UnitStrindex] == profilingSampleUnit;
    }

    private static bool ContainPeriod(Profile profile, ProfilesDictionary dictionary, string profilingSampleType, string profilingSampleUnit, long period)
    {
        return dictionary.StringTable[profile.PeriodType.TypeStrindex] == profilingSampleType &&
            dictionary.StringTable[profile.PeriodType.UnitStrindex] == profilingSampleUnit &&
            profile.Period == period;
    }

    private static List<string> CreateExpectedStackTrace()
    {
        List<string> stackTrace =
        [
            "System.Threading.Thread.Sleep(System.TimeSpan)",
            "TestApplication.ContinuousProfiler.Fs.ClassFs.methodFs(System.String)",
            "TestApplication.ContinuousProfiler.Vb.ClassVb.MethodVb(System.String)",
            "My.Custom.Test.Namespace.TestDynamicClass.TryInvoke(System.Dynamic.InvokeBinder, System.Object[], System.Object\u0026)",
            "System.Dynamic.UpdateDelegates.UpdateAndExecuteVoid3[T0, T1, T2](System.Runtime.CompilerServices.CallSite, T0, T1, T2)",
#if NETFRAMEWORK
            "Unknown_Native_Function(unknown)",
#endif
            "My.Custom.Test.Namespace.ClassENonStandardCharacters\u0104\u0118\u00D3\u0141\u017B\u0179\u0106\u0105\u0119\u00F3\u0142\u017C\u017A\u015B\u0107\u011C\u0416\u13F3\u2CC4\u02A4\u01CB\u2093\u06BF\u0B1F\u0D10\u1250\u3023\u203F\u0A6E\u1FAD_\u00601.GenericMethodDFromGenericClass[TMethod, TMethod2](TClass, TMethod, TMethod2)",
            "My.Custom.Test.Namespace.ClassD`21.MethodD(T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, Unknown)",
            "My.Custom.Test.Namespace.GenericClassC`1.GenericMethodCFromGenericClass[T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20](T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, Unknown)",
            "My.Custom.Test.Namespace.GenericClassC`1.GenericMethodCFromGenericClass(T)"
        ];

#if NETFRAMEWORK || DEBUG
        stackTrace.Add("Unknown_Native_Function(unknown)");
#else
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            stackTrace.Add("Unknown_Native_Function(unknown)");
        }
#endif
        stackTrace.Add("My.Custom.Test.Namespace.ClassA.InternalClassB`2.DoubleInternalClassB.TripleInternalClassB`1.MethodB[T2](System.Int32, T3[], T2, T4, System.Collections.Generic.IList`1[T1], System.Collections.Generic.IList`1[System.String])");
        stackTrace.Add("My.Custom.Test.Namespace.ClassA.<MethodAOthers>g__Action|7_0[T](System.Int32)");
        stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAOthers[T](System.String, System.Object, My.Custom.Test.Namespace.CustomClass, My.Custom.Test.Namespace.CustomStruct, My.Custom.Test.Namespace.CustomClass[], My.Custom.Test.Namespace.CustomStruct[], System.Collections.Generic.List`1[T])");
        stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAPointer(System.Int32*)");
        stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAFloats(System.Single, System.Double)");
        stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAInts(System.UInt16, System.Int16, System.UInt32, System.Int32, System.UInt64, System.Int64, System.IntPtr, System.UIntPtr)");
        stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodABytes(System.Boolean, System.Char, System.SByte, System.Byte)");
        stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodA()");

        return stackTrace;
    }

    private static bool ContainStackTraceForClassHierarchy(Profile profile, ProfilesDictionary dictionary, string expectedStackTrace)
    {
        foreach (var sample in profile.Samples)
        {
            var stackIndex = sample.StackIndex;
            if (stackIndex <= 0 || stackIndex >= dictionary.StackTable.Count)
            {
                continue;
            }

            var stack = dictionary.StackTable[stackIndex];
            var frames = GetFrameNames(stack, dictionary);
            var stackTrace = string.Join("\n", frames);

#if NET
            if (stackTrace.Contains(expectedStackTrace, StringComparison.Ordinal))
#else
            if (stackTrace.Contains(expectedStackTrace))
#endif
            {
                return true;
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
