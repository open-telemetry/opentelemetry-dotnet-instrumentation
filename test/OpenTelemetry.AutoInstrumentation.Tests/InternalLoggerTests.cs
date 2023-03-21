// <copyright file="InternalLoggerTests.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class InternalLoggerTests
{
    [Fact]
    public void WhenLogLevelIsConfigured_Then_OnlyEntriesWithEqualOrLowerLevelAreForwardedToSink()
    {
        var sink = new TestSink();
        var logger = new InternalLogger(sink, LogLevel.Information);

        // should be logged as it matches configured log level
        logger.Information("info message", false);
        // should be ignored, because LogLevel.Debug > LogLevel.Information
        logger.Debug("debug message", false);

        sink.Messages.Count.Should().Be(1);
        sink.Messages[0].Should().Contain("[Information] info message");
    }
}
