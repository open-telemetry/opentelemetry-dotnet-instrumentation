using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers
{
    internal static class HealthzHelper
    {
        public static async Task<bool> TestHealtzAsync(string healthzUrl, string logPrefix, ITestOutputHelper output)
        {
            output.WriteLine($"{logPrefix} healthz endpoint: {healthzUrl}");
            HttpClient client = new();

            for (int retry = 0; retry < 5; retry++)
            {
                HttpResponseMessage response;

                try
                {
                    response = await client.GetAsync(healthzUrl);
                }
                catch (TaskCanceledException)
                {
                    response = null;
                }

                if (response?.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }

                output.WriteLine($"{logPrefix} healthz failed {retry + 1}/5");
                await Task.Delay(TimeSpan.FromSeconds(4));
            }

            return false;
        }
    }
}
