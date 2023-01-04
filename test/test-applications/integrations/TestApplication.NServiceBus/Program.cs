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

using System.Diagnostics;
using TestApplication.NServiceBus;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var endpointConfiguration = new EndpointConfiguration("TestApplication.NServiceBus");

var learningTransport = new LearningTransport { StorageDirectory = Path.GetTempPath() };
endpointConfiguration.UseTransport(learningTransport);

using var cancellation = new CancellationTokenSource();
var endpointInstance = await Endpoint.Start(endpointConfiguration, cancellation.Token);

try
{
    await endpointInstance.SendLocal(new TestMessage(), cancellation.Token);

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
        Process.GetCurrentProcess().WaitForExit();
    }
}
finally
{
    await endpointInstance.Stop(cancellation.Token);
}
