// <copyright file="HealthzHelper.cs" company="OpenTelemetry Authors">
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

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

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
