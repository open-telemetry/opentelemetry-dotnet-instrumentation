// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.ServiceModel;
using TestApplication.Shared;

namespace TestApplication.Wcf.Server.NetFramework;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var httpPort = GetHttpPort(args);
        var tcpPort = GetTcpPort(args);

        var httpAddress = $"http://127.0.0.1:{httpPort}/Telemetry";
        var tcpAddress = $"net.tcp://127.0.0.1:{tcpPort}/Telemetry";

        try
        {
            using var serviceHost = new ServiceHost(typeof(StatusService));

            // Configure BasicHttpBinding endpoint
            var basicHttpBinding = new BasicHttpBinding { Security = { Mode = BasicHttpSecurityMode.None } };
            serviceHost.AddServiceEndpoint(typeof(IStatusServiceContract), basicHttpBinding, httpAddress);

            // Configure NetTcpBinding endpoint
            var netTcpBinding = new NetTcpBinding { Security = { Mode = SecurityMode.None } };
            serviceHost.AddServiceEndpoint(typeof(IStatusServiceContract), netTcpBinding, tcpAddress);

            serviceHost.Open();

            Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] Server waiting for calls");
            Console.WriteLine($"HTTP endpoint: {httpAddress}");
            Console.WriteLine($"NetTcp endpoint: {tcpAddress}");

            using var manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne();
        }
        catch (Exception e)
        {
            Console.WriteLine($"ServerException: {e}");
        }

        Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] WCFServer: exiting main()");
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
