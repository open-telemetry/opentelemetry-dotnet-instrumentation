// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Net;
using Microsoft.AspNetCore.Http;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

internal static class MockCollectorHealthZ
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(1)
    };

    public static PathHandler CreateHealthZHandler()
    {
        return new PathHandler(HandleHealthZRequests, "/healthz");
    }

    public static async Task WarmupHealthZEndpoint(ITestOutputHelper output, string host, int port)
    {
        var finalHost = host == "*" ? "localhost" : host;

        var healthZUrl = new Uri($"http://{finalHost}:{port}/healthz");
        const int maxAttempts = 10;
        for (var i = 1; i <= maxAttempts; ++i)
        {
            try
            {
                var response = await HttpClient.GetAsync(healthZUrl).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types. Catching all exceptions to retry.
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types. Catching all exceptions to retry.
            {
                output.WriteLine($"Exception while calling {healthZUrl}: {e.Message}. Attempt: {i}");
            }

            if (i == maxAttempts)
            {
                throw new InvalidOperationException($"Failed to warm up healthz endpoint at {healthZUrl} after {maxAttempts} attempts.");
            }
        }
    }

    private static async Task HandleHealthZRequests(HttpContext context)
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("OK").ConfigureAwait(false);
    }
}
#endif
