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

using System;
using System.ServiceModel;
using System.Threading;

namespace TestApplication.Wcf.Server.NetFramework;

internal static class Program
{
    public static void Main()
    {
        try
        {
            var serviceHost = new ServiceHost(typeof(StatusService));
            serviceHost.Open();

            Console.WriteLine($"[{DateTimeOffset.UtcNow:yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'}] Server waiting for calls");

            var manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne();
        }
        catch (Exception e)
        {
            Console.WriteLine($"ServerException: {e}");
        }

        Console.WriteLine($"[{DateTimeOffset.UtcNow:yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'}] WCFServer: exiting main()");
    }
}
