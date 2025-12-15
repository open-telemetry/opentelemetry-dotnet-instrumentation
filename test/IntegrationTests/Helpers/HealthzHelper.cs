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
        using HttpClient client = new();

        const int intervalMilliseconds = 500;
        const int maxMillisecondsToWait = 15_000;
        const int maxRetries = maxMillisecondsToWait / intervalMilliseconds;
        for (var retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                var response = await client.GetAsync(new Uri(healthzUrl)).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }

                output.WriteLine($"Healthz endpoint returned HTTP status code: {response.StatusCode}");
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                output.WriteLine($"Healthz endpoint call failed: {ex.Message}");
            }

            await Task.Delay(intervalMilliseconds).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Healthz endpoint never returned OK: {healthzUrl}");
    }
}
