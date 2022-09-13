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
            try
            {
                ServiceHost serviceHost = new ServiceHost(typeof(StatusService));
                serviceHost.Open();

                while (StatusService.TimesHit != 2)
                {
                    Console.WriteLine("Server waiting for calls");
                    Thread.Sleep(1000);
                }

                serviceHost.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"ServerException: nested try-catch {e}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"ServerException: top-level try-catch {e}");
        }

        Console.WriteLine("WCFServer: exiting main()");
    }
}
