// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace TestApplication.Log4NetBridge;

internal static class Program
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Program));
    private static readonly ActivitySource Source = new("TestApplication.Log4NetBridge");

    private static void Main(string[] args)
    {
        using var activity = Source.StartActivity("ManuallyStarted");
        Log.InfoFormat("Hello, {0}!", "world");
        Log.Error("Exception occured", new Exception());
    }
}
