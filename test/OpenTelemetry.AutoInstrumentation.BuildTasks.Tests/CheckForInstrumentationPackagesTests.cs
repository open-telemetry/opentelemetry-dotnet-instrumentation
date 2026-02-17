// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Build.Framework;
using NSubstitute;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.BuildTasks.Tests;

public class CheckForInstrumentationPackagesTests
{
    public static TheoryData<ITaskItem[]?> LogicallyEmptyITaskItemArray()
    {
        var theoryData = new TheoryData<ITaskItem[]?>
        {
            null,
            Array.Empty<ITaskItem>()
        };

        return theoryData;
    }

    [Theory]
    [MemberData(nameof(LogicallyEmptyITaskItemArray))]
    public void NoDataInRuntimeLocalCopyItemsTest(ITaskItem[]? runtimeCopyLocalItems)
    {
        var sut = new TaskWithMockBuildEngine();

        sut.Task.RuntimeCopyLocalItems = runtimeCopyLocalItems;
        Assert.True(sut.Task.Execute());

        var messageEventArgsList = sut.MessageEventArgsList;
        var buildMessageEventArgs = Assert.Single(messageEventArgsList);
        Assert.Equal("OpenTelemetry.AutoInstrumentation: empty RuntimeCopyLocalItems, skipping check for instrumentation packages.", buildMessageEventArgs.Message);
    }

    [Fact]
    public void EmptyInstrumentationTargetItemsTest()
    {
        var sut = new TaskWithMockBuildEngine();

        sut.Task.InstrumentationTargetItems = Array.Empty<ITaskItem>();

        sut.Task.RuntimeCopyLocalItems = BuildMockRuntimeCopyLocalItems(new (string, string)[]
        {
            ("Test.Package.A", "1.2.0")
        });

        Assert.True(sut.Task.Execute());

        Assert.Empty(sut.MessageEventArgsList);
    }

    [Fact]
    public void NoPackageToBeInstrumentedTest()
    {
        var sut = new TaskWithMockBuildEngine();

        sut.Task.RuntimeCopyLocalItems = BuildMockRuntimeCopyLocalItems(new (string, string)[]
            {
                ("Test.Package.A", "1.2.0"),
                ("Test.Package.A.Instrumentation", "1.1.0"),
                ("Test.Package.B", "2.2.0"),
                ("Test.Package.D", "3.3.0")
            });

        sut.Task.InstrumentationTargetItems = BuildMockInstrumentationTargetItems(new (string, string, string, string)[]
            {
                // Matches A package Id but already has instrumentation package.
                ("Test.Package.A", "[1.1.0, 2.0.0)", "Test.Package.A.Instrumentation", "1.1.0"),
                // Matches B package Id but not range.
                ("Test.Package.B", "[1.0.0, 2.0.0)", "Test.Package.B.Instrumentation", "1.0.0"),
                // Doesn't match any package Id.
                ("Test.Package.C", "[1.0.0, 2.0.0)", "Test.Package.C.Instrumentation", "1.0.0")
            });

        Assert.True(sut.Task.Execute());

        var expectedMessages = new string[]
        {
            "OpenTelemetry.AutoInstrumentation: project already references 'Test.Package.A.Instrumentation' version 1.1.0 " +
                "required to instrument 'Test.Package.A' version [1.1.0, 2.0.0).",
            "OpenTelemetry.AutoInstrumentation: project references 'Test.Package.B' but not in the range [1.0.0, 2.0.0), " +
                "no need for the instrumentation package 'Test.Package.B.Instrumentation' version 1.0.0.",
            "OpenTelemetry.AutoInstrumentation: project doesn't reference 'Test.Package.C', " +
                "no need for the instrumentation package 'Test.Package.C.Instrumentation' version 1.0.0."
        };
        var messageEventArgsList = sut.MessageEventArgsList;
        Assert.Equal(expectedMessages.Length, messageEventArgsList.Count);
        Assert.All(messageEventArgsList, (eventArgs, i) => Assert.Equal(expectedMessages[i], eventArgs.Message));
    }

    [Fact]
    public void MissingInstrumentationPackageTest()
    {
        var sut = new TaskWithMockBuildEngine();
        sut.Task.RuntimeCopyLocalItems = BuildMockRuntimeCopyLocalItems(new (string, string)[]
            {
                    ("Test.Package.A", "1.2.0"),
                    ("Test.Package.D", "3.3.0")
            });

        sut.Task.InstrumentationTargetItems = BuildMockInstrumentationTargetItems(new (string, string, string, string)[]
            {
                // Matches A package Id and range, but is not in the project references.
                ("Test.Package.A", "[1.1.0, 2.0.0)", "Test.Package.A.Instrumentation", "1.1.0")
            });

        Assert.False(sut.Task.Execute());

        Assert.Empty(sut.MessageEventArgsList);

        var errorEventArgs = Assert.Single(sut.ErrorEventArgsList);

        var expectedMessage =
            "OpenTelemetry.AutoInstrumentation: add a reference to the instrumentation package 'Test.Package.A.Instrumentation' " +
            "version 1.1.0 or add 'Test.Package.A' to the property 'SkippedInstrumentations' to suppress this error.";

        Assert.Equal(expectedMessage, errorEventArgs.Message);
    }

    private static ITaskItem[] BuildMockRuntimeCopyLocalItems((string NuGetPackageId, string NuGetPackageVersion)[] source)
    {
        var taskItems = new ITaskItem[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            var (nuGetPackageId, nuGetPackageVersion) = source[i];
            var mockTaskItem = Substitute.For<ITaskItem>();
            mockTaskItem.GetMetadata("NuGetPackageId").Returns(nuGetPackageId);
            mockTaskItem.GetMetadata("NuGetPackageVersion").Returns(nuGetPackageVersion);
            taskItems[i] = mockTaskItem;
        }

        return taskItems;
    }

    private static ITaskItem[] BuildMockInstrumentationTargetItems(
        (string ItemSpec, string TargetNuGetPackageVersionRange, string InstrumentationNuGetPackageId, string InstrumentationNuGetPackageVersion)[] source)
    {
        var taskItems = new ITaskItem[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            var (itemSpec, targetNuGetPackageVersionRange, instrumentationNuGetPackageId, instrumentationNuGetPackageVersion) = source[i];
            var mockTaskItem = Substitute.For<ITaskItem>();
            mockTaskItem.ItemSpec.Returns(itemSpec);
            mockTaskItem.GetMetadata("TargetNuGetPackageVersionRange").Returns(targetNuGetPackageVersionRange);
            mockTaskItem.GetMetadata("InstrumentationNuGetPackageId").Returns(instrumentationNuGetPackageId);
            mockTaskItem.GetMetadata("InstrumentationNuGetPackageVersion").Returns(instrumentationNuGetPackageVersion);
            taskItems[i] = mockTaskItem;
        }

        return taskItems;
    }

    private sealed class TaskWithMockBuildEngine
    {
        private readonly List<BuildMessageEventArgs> _buildMessageEventArgsList = [];
        private readonly List<BuildErrorEventArgs> _buildErrorEventArgsList = [];

        public TaskWithMockBuildEngine()
        {
            var task = new CheckForInstrumentationPackages();

            var mockBuildEngine = Substitute.For<IBuildEngine>();
            task.BuildEngine = mockBuildEngine;

            // Capture the BuildMessageEventArgs argument passed to LogMessageEvent.
            mockBuildEngine.When(x => x.LogMessageEvent(Arg.Any<BuildMessageEventArgs>()))
                .Do(x => _buildMessageEventArgsList.Add(x.ArgAt<BuildMessageEventArgs>(0)));

            // Capture the BuildErrorEventArgs argument passed to LogErrorEvent.
            mockBuildEngine.When(x => x.LogErrorEvent(Arg.Any<BuildErrorEventArgs>()))
                .Do(x => _buildErrorEventArgsList.Add(x.ArgAt<BuildErrorEventArgs>(0)));

            Task = task;
            MockBuildEngine = mockBuildEngine;
        }

        public CheckForInstrumentationPackages Task { get; }

        public IBuildEngine MockBuildEngine { get; }

        public IReadOnlyList<BuildMessageEventArgs> MessageEventArgsList => _buildMessageEventArgsList;

        public IReadOnlyList<BuildErrorEventArgs> ErrorEventArgsList => _buildErrorEventArgsList;
    }
}
