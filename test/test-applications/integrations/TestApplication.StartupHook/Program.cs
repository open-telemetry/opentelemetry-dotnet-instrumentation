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

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using CommandLine;

namespace TestApplication.StartupHook
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args);

            options.WithParsed<Options>(o =>
            {
                if (o.Traces)
                {
                    EmitTraces();
                }

                if (o.Metrics)
                {
                    EmitMetrics();
                }
            });
        }

        private static void EmitTraces()
        {
            var myActivitySource = new ActivitySource("TestApplication.StartupHook", "1.0.0");

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
            var myMeter = new Meter("MyCompany.MyProduct.MyLibrary", "1.0");
            var myFruitCounter = myMeter.CreateCounter<int>("MyFruitCounter");

            myFruitCounter.Add(1, new KeyValuePair<string, object>("name", "apple"));
        }

        public class Options
        {
            [Option('t', "traces", HelpText = "Emit spans.")]
            public bool Traces { get; set; }

            [Option('m', "metrics", HelpText = "Emit metrics.")]
            public bool Metrics { get; set; }
        }
    }
}
