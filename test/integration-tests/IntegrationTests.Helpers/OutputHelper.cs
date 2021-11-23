using System;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers
{
    public static class OutputHelper
    {
        public static void WriteResult(this ITestOutputHelper outputHelper, ProcessHelper processHelper)
        {
            processHelper.Drain();

            string standardOutput = processHelper.StandardOutput;
            if (!string.IsNullOrWhiteSpace(standardOutput))
            {
                outputHelper.WriteLine($"StandardOutput:{Environment.NewLine}{standardOutput}");
            }

            string standardError = processHelper.ErrorOutput;
            if (!string.IsNullOrWhiteSpace(standardError))
            {
                outputHelper.WriteLine($"StandardError:{Environment.NewLine}{standardError}");
            }
        }
    }
}
