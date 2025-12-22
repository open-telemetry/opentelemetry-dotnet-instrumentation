// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using TestApplication.NServiceBus;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var endpointConfiguration = new EndpointConfiguration("TestApplication.NServiceBus");

var learningTransport = new LearningTransport { StorageDirectory = Path.GetTempPath() };
endpointConfiguration.UseTransport(learningTransport);
endpointConfiguration.UseSerialization<XmlSerializer>();

using var cancellation = new CancellationTokenSource();
var endpointInstance = await Endpoint.Start(endpointConfiguration, cancellation.Token).ConfigureAwait(false);

try
{
    await endpointInstance.SendLocal(new TestMessage(), cancellation.Token).ConfigureAwait(false);

    // The "LONG_RUNNING" environment variable is used by tests that access/receive
    // data that takes time to be produced.
    var longRunning = Environment.GetEnvironmentVariable("LONG_RUNNING");
    if (longRunning == "true")
    {
        // In this case it is necessary to ensure that the test has a chance to read the
        // expected data, only by keeping the application alive for some time that can
        // be ensured. Anyway, tests that set "LONG_RUNNING" env var to true are expected
        // to kill the process directly.
        Console.WriteLine("LONG_RUNNING is true, waiting for process to be killed...");

#if NET
        await Process.GetCurrentProcess().WaitForExitAsync().ConfigureAwait(false);
#else
        Process.GetCurrentProcess().WaitForExit();
#endif
    }
}
finally
{
    await endpointInstance.Stop(cancellation.Token).ConfigureAwait(false);
}
