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

using System.Net;
using System.Text;

namespace TestApplication.Http.NetFramework;

public class Program
{
    public static void Main(string[] args)
    {
        using var listener = new TestServer("/test/");
        var address = $"http://localhost:{listener.Port}";

        var request = (HttpWebRequest)WebRequest.Create($"{address}/test");
        request.Method = "POST";
        request.ContentType = "text/plain";

        using (Stream requestStream = request.GetRequestStream())
        {
            var content = Encoding.UTF8.GetBytes("Ping");

            requestStream.Write(content, 0, content.Length);
        }

        var response = request.GetResponse();

        using (var responseStream = response.GetResponseStream())
        using (var responseReader = new StreamReader(responseStream))
        {
            var text = responseReader.ReadToEnd();
            Console.WriteLine("[CLIENT] Received: {0}", text);
        }
    }
}
