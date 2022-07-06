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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Threading;

namespace TestApplication.Smoke
{
    public class Program
    {
        public static readonly string SourceName = "MyCompany.MyProduct.MyLibrary";

        public static void Main(string[] args)
        {
            EmitTraces();
            EmitMetrics();

            var longRunning = Environment.GetEnvironmentVariable("LONG_RUNNING");
            while (longRunning == "true")
            {
                Thread.Yield();
            }
        }

        private static void EmitTraces()
        {
            var myActivitySource = new ActivitySource(SourceName, "1.0.0");

            using (var activity = myActivitySource.StartActivity("SayHello"))
            {
                activity?.SetTag("foo", 1);
                activity?.SetTag("bar", "Hello, World!");
                activity?.SetTag("baz", new int[] { 1, 2, 3 });
            }

            var client = new HttpClient();
            client.GetStringAsync("http://httpstat.us/200").Wait();
        }

        private static void EmitMetrics()
        {
            var myMeter = new Meter(SourceName, "1.0");
            var myFruitCounter = myMeter.CreateCounter<int>("MyFruitCounter");

            myFruitCounter.Add(1, new KeyValuePair<string, object>("name", "apple"));
        }
    }
}
