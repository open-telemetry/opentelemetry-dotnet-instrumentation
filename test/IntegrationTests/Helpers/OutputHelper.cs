// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

internal static class OutputHelper
{
    public static void WriteResult(this ITestOutputHelper outputHelper, ProcessHelper processHelper)
    {
        processHelper.Drain();

        var standardOutput = processHelper.StandardOutput;
        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            outputHelper.WriteLine("StandardOutput:");
            outputHelper.WriteLine(standardOutput);
        }

        var standardError = processHelper.ErrorOutput;
        if (!string.IsNullOrWhiteSpace(standardError))
        {
            outputHelper.WriteLine("StandardError:");
            outputHelper.WriteLine(standardError);
        }
    }
}
