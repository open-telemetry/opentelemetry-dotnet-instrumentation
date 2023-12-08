// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// Example usage:
//     dotnet run http://localhost:5200
if (args.Length != 1)
{
    Console.WriteLine(@"URL missing");
    return 2;
}

var url = args[0];
using var httpClient = new HttpClient();
while (true)
{
    try
    {
        var content = await httpClient.GetStringAsync(url);
        Console.WriteLine(content);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine(ex.Message);
    }

    Thread.Sleep(5000);
}
