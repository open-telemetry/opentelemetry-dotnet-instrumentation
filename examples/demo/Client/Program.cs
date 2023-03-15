// <copyright file="Program.cs" company="OpenTelemetry Authors">
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

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

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
    Thread.Sleep(5000);
    try
    {
        var content = await httpClient.GetStringAsync(url);
        Console.WriteLine(content);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine(ex.Message);
    }
}
