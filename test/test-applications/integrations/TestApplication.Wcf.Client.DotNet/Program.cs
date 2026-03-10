// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using TestApplication.Shared;

namespace TestApplication.Wcf.Client.DotNet;

internal static class Program
{
    private static readonly ActivitySource Source = new(Assembly.GetExecutingAssembly().GetName().Name!, "1.0.0.0");

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var tcpPort = GetTcpPort(args);
        var httpPort = GetHttpPort(args);

        var netTcpAddress = $"net.tcp://127.0.0.1:{tcpPort}/Telemetry";
        var httpAddress = $"http://127.0.0.1:{httpPort}/Telemetry";

        using var parent = Source.StartActivity("Parent");
        try
        {
            Console.WriteLine("=============NetTcp===============");
            await CallService(netTcpAddress, new NetTcpBinding(SecurityMode.None)).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // netTcp fails on open when there is no endpoint
        }

        Console.WriteLine("=============Http===============");
        await CallService(httpAddress, new BasicHttpBinding()).ConfigureAwait(false);
        using var sibling = Source.StartActivity("Sibling");
    }

    private static async Task CallService(string address, Binding binding)
    {
        // Note: Best practice is to re-use your client/channel instances.
        // This code is not meant to illustrate best practices, only the
        // instrumentation.
        using var client = new StatusServiceClient(binding, new EndpointAddress(new Uri(address)));
        await client.OpenAsync().ConfigureAwait(false);

        try
        {
            try
            {
                Console.WriteLine("Task-based Asynchronous Pattern call");
                var rq = new StatusRequest { Status = "1" };
                var response = await client.PingAsync(rq).ConfigureAwait(false);

                Console.WriteLine(
                    $"[{DateTimeOffset.UtcNow:o}] Request with status {rq.Status}. Server returned: {response?.ServerTime:o}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        finally
        {
            try
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    await client.CloseAsync().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }
        }
    }

    private static int GetTcpPort(string[] args)
    {
        if (args.Length > 1)
        {
            return int.Parse(args[1], CultureInfo.InvariantCulture);
        }

        return 9090;
    }

    private static int GetHttpPort(string[] args)
    {
        if (args.Length > 3)
        {
            return int.Parse(args[3], CultureInfo.InvariantCulture);
        }

        return 9009;
    }
}
