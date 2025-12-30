// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ServiceModel;
using TestApplication.Shared;

namespace TestApplication.Wcf.Server.NetFramework;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        try
        {
            using var serviceHost = new ServiceHost(typeof(StatusService));
            serviceHost.Open();

            Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] Server waiting for calls");

            using var manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne();
        }
        catch (Exception e)
        {
            Console.WriteLine($"ServerException: {e}");
        }

        Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] WCFServer: exiting main()");
    }
}
