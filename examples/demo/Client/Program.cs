// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// Example usage:
//     dotnet run http://localhost:5200
if (args.Length != 1)
{
#pragma warning disable CA1303 // Do not pass literals as localized parameters
    Console.WriteLine("URL missing");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
    return 2;
}

var url = args[0];
using var httpClient = new HttpClient();
while (true)
{
    try
    {
        var content = await httpClient.GetStringAsync(new Uri(url)).ConfigureAwait(false);
        Console.WriteLine(content);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine(ex.Message);
    }

    await Task.Delay(5000).ConfigureAwait(false);
}
