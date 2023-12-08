// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
