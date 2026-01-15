// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using TestApplication.Shared;

namespace TestApplication.Wcf.Client.NetFramework;

internal static class Program
{
    private static readonly ActivitySource Source = new(Assembly.GetExecutingAssembly().GetName().Name, "1.0.0.0");

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        string netTcpAddress;
        string httpAddress;
        if (args.Length == 0)
        {
            // Self-hosted service addresses
            netTcpAddress = "net.tcp://127.0.0.1:9090/Telemetry";
            httpAddress = "http://127.0.0.1:9009/Telemetry";
        }
        else if (args.Length == 2)
        {
            // Addresses of a service hosted in IIS inside container
            netTcpAddress = $"net.tcp://localhost:{args[0]}/StatusService.svc";
            httpAddress = $"http://localhost:{args[1]}/StatusService.svc";
        }
        else if (args.Length == 4)
        {
            // Self-hosted service addresses
            netTcpAddress = $"net.tcp://127.0.0.1:{args[1]}/Telemetry";
            httpAddress = $"http://127.0.0.1:{args[3]}/Telemetry";
        }
        else
        {
            throw new InvalidOperationException(
                "TestApplication.Wcf.Client.NetFramework application requires either 0, 2, or 4 arguments.");
        }

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

                // Task.Yield() is required in order for successive calls
                // not to timeout, this seems to be a known issue for e.g console apps
                // making WCF sync calls after an async call
                await Task.Yield();

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
        if (args.Length > 0)
        {
            return int.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
        }

        return 9090;
    }

    private static int GetHttpPort(string[] args)
    {
        if (args.Length > 1)
        {
            return int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
        }

        return 9009;
    }
}
