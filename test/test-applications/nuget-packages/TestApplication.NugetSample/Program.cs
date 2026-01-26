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


using System.Globalization;
#if NETFRAMEWORK
using System.Net.Http;
#endif

using var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(10);
if (args.Length != 2)
{
    throw new InvalidOperationException("Missing arguments. Provide test server port with --test-server-port <test-server-port>");
}
try
{
    var testServerPort = int.Parse(args[1], CultureInfo.InvariantCulture);
    var response = await httpClient.GetAsync(new Uri($"http://localhost:{testServerPort}/test/")).ConfigureAwait(false);
    Console.WriteLine(response.StatusCode);
}
catch (Exception e)
{
    Console.WriteLine(e);
}
