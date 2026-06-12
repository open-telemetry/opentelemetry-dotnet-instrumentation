// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Instrumentations;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations;

public class ActivityTraceFlagsExtensionsTests
{
    [Theory]
    [InlineData(0x00, "00")]
    [InlineData(0x01, "01")]
    [InlineData(0x02, "02")]
    [InlineData(0x03, "03")]
    [InlineData(0x0a, "0a")]
    [InlineData(0x10, "10")]
    [InlineData(0xf0, "f0")]
    [InlineData(0xff, "ff")]
    public void W3CFormatActivityTraceFlags_ReturnsLowerCaseHexFormat(int flags, string expected)
    {
        var result = ((ActivityTraceFlags)flags).W3CFormatActivityTraceFlags();

        Assert.Equal(expected, result);
    }
}
