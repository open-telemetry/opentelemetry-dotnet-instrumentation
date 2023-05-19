// <copyright file="CheckForInstrumentationPackagesTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using FluentAssertions;
using Microsoft.Build.Framework;
using Moq;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.BuildTasks.Tests;

public class CheckForInstrumentationPackagesTests
{
    public static IEnumerable<object?[]> LogicallyEmptyITaskItemArray =>
        new object?[][]
        {
            new object?[] { null },
            new object?[] { Array.Empty<ITaskItem[]>() }
        };

    [Theory]
    [MemberData(nameof(LogicallyEmptyITaskItemArray))]
    public void NoDataInRuntimeLocalCopyItemsTest(ITaskItem[]? runtimeCopyLocalItems)
    {
        var sut = new TaskWithMockBuildEngine();

        sut.Task.RuntimeCopyLocalItems = runtimeCopyLocalItems;
        sut.Task.Execute().Should().BeTrue();

        var messageEventArgsList = sut.MessageEventArgsList;
        messageEventArgsList.Should().HaveCount(1);
        messageEventArgsList[0].Message.Should().Be(
            "OpenTelemetry.AutoInstrumentation: empty RuntimeCopyLocalItems, skipping check for instrumentation packages.");
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

        sut.Task.Execute().Should().BeTrue();

        sut.MessageEventArgsList.Should().BeEmpty();
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

        sut.Task.Execute().Should().BeTrue();

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
        messageEventArgsList.Should().HaveCount(expectedMessages.Length);
        messageEventArgsList.Select((eventArgs, i) => eventArgs.Message.Should().Be(expectedMessages[i])).ToList();
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

        sut.Task.Execute().Should().BeFalse();

        sut.MessageEventArgsList.Should().BeEmpty();

        sut.ErrorEventArgsList.Should().HaveCount(1);
        sut.ErrorEventArgsList[0].Message.Should().Be(
            "OpenTelemetry.AutoInstrumentation: add a reference to the instrumentation package 'Test.Package.A.Instrumentation' " +
            "version 1.1.0 or add 'Test.Package.A' to the property 'SkippedInstrumentations' to suppress this error.");
    }

    private static ITaskItem[] BuildMockRuntimeCopyLocalItems((string NuGetPackageId, string NuGetPackageVersion)[] source)
    {
        var taskItems = new ITaskItem[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            var (nuGetPackageId, nuGetPackageVersion) = source[i];
            var mockTaskItem = new Mock<ITaskItem>();
            mockTaskItem.Setup(x => x.GetMetadata("NuGetPackageId")).Returns(nuGetPackageId);
            mockTaskItem.Setup(x => x.GetMetadata("NuGetPackageVersion")).Returns(nuGetPackageVersion);
            taskItems[i] = mockTaskItem.Object;
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
            var mockTaskItem = new Mock<ITaskItem>();
            mockTaskItem.Setup(x => x.ItemSpec).Returns(itemSpec);
            mockTaskItem.Setup(x => x.GetMetadata("TargetNuGetPackageVersionRange")).Returns(targetNuGetPackageVersionRange);
            mockTaskItem.Setup(x => x.GetMetadata("InstrumentationNuGetPackageId")).Returns(instrumentationNuGetPackageId);
            mockTaskItem.Setup(x => x.GetMetadata("InstrumentationNuGetPackageVersion")).Returns(instrumentationNuGetPackageVersion);
            taskItems[i] = mockTaskItem.Object;
        }

        return taskItems;
    }

    private class TaskWithMockBuildEngine
    {
        private List<BuildMessageEventArgs> _buildMessageEventArgsList = new List<BuildMessageEventArgs>();
        private List<BuildErrorEventArgs> _buildErrorEventArgsList = new List<BuildErrorEventArgs>();

        public TaskWithMockBuildEngine()
        {
            var task = new CheckForInstrumentationPackages();

            var mockBuildEngine = new Mock<IBuildEngine>();
            task.BuildEngine = mockBuildEngine.Object;

            // Capture the BuildMessageEventArgs argument passed to LogMessageEvent.
            mockBuildEngine.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                .Callback<BuildMessageEventArgs>(_buildMessageEventArgsList.Add);

            // Capture the BuildErrorEventArgs argument passed to LogErrorEvent.
            mockBuildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(_buildErrorEventArgsList.Add);

            Task = task;
            MockBuildEngine = mockBuildEngine;
        }

        public CheckForInstrumentationPackages Task { get; }

        public Mock<IBuildEngine> MockBuildEngine { get; }

        public IReadOnlyList<BuildMessageEventArgs> MessageEventArgsList => _buildMessageEventArgsList;

        public IReadOnlyList<BuildErrorEventArgs> ErrorEventArgsList => _buildErrorEventArgsList;
    }
}
