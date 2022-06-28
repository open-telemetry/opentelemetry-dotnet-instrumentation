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
                if (o.Trace)
                {
                    GenerateTraceData();
                }

                if (o.Metrics)
                {
                    GenerateMetricsData();
                }
            });
        }

        private static void GenerateTraceData()
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

        private static void GenerateMetricsData()
        {
            var myMeter = new Meter("MyCompany.MyProduct.MyLibrary", "1.0");
            var myFruitCounter = myMeter.CreateCounter<long>("MyFruitCounter");

            myFruitCounter.Add(1, new("name", "apple"), new("color", "red"));
            myFruitCounter.Add(2, new("name", "lemon"), new("color", "yellow"));
            myFruitCounter.Add(1, new("name", "lemon"), new("color", "yellow"));
            myFruitCounter.Add(2, new("name", "apple"), new("color", "green"));
            myFruitCounter.Add(5, new("name", "apple"), new("color", "red"));
            myFruitCounter.Add(4, new("name", "lemon"), new("color", "yellow"));
        }

        public class Options
        {
            [Option('t', "Trace", Required = false, HelpText = "Set this option to collect trace messages.")]
            public bool Trace { get; set; }

            [Option('l', "verbose", Required = false, HelpText = "Set this option to collect logs.")]
            public bool Logs { get; set; }

            [Option('m', "verbose", Required = false, HelpText = "Set this option to collect metrics.")]
            public bool Metrics { get; set; }
        }
    }
}
