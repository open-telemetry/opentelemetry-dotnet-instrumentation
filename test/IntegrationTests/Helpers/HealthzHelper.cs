// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
#if NETFRAMEWORK
using System.Net.Http;
#endif
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

internal static class HealthzHelper
{
    public static async Task TestAsync(string healthzUrl, ITestOutputHelper output)
    {
        output.WriteLine($"Testing healthz endpoint: {healthzUrl}");
        HttpClient client = new();

        var intervalMilliseconds = 500;
        var maxMillisecondsToWait = 15_000;
        var maxRetries = maxMillisecondsToWait / intervalMilliseconds;
        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                var response = await client.GetAsync(healthzUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }

                output.WriteLine($"Healthz endpoint returned HTTP status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                output.WriteLine($"Healthz endpoint call failed: {ex.Message}");
            }

            await Task.Delay(intervalMilliseconds);
        }

        throw new InvalidOperationException($"Healthz endpoint never returned OK: {healthzUrl}");
    }
}
