// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Text;
using TestApplication.Shared;

namespace TestApplication.Http.NetFramework;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
        using var listener = new TestServer("/test/");
        var address = $"http://localhost:{listener.Port}";

        var request = (HttpWebRequest)WebRequest.Create(new Uri($"{address}/test"));
        request.Method = "POST";
        request.ContentType = "text/plain";
        request.Headers.Add("Custom-Request-Test-Header1", "Test-Value1");
        request.Headers.Add("Custom-Request-Test-Header2", "Test-Value2");
        request.Headers.Add("Custom-Request-Test-Header3", "Test-Value3");

        using (var requestStream = request.GetRequestStream())
        {
            var content = Encoding.UTF8.GetBytes("Ping");

            requestStream.Write(content, 0, content.Length);
        }

        var response = request.GetResponse();

        using var responseStream = response.GetResponseStream();
        using var responseReader = new StreamReader(responseStream);
        var text = responseReader.ReadToEnd();
        Console.WriteLine("[CLIENT] Received: {0}", text);
    }
}
