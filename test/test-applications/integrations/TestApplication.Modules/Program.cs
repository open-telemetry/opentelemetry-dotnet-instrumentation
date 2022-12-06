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
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TestApplication.Shared;

namespace TestApplication.Smoke;

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        if (args.Length < 2)
        {
            throw new ArgumentException("Temp path is not provided. Use '--temp-path /my/path/to/temp_file'");
        }

        var otelLibs = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(x => x.GetName().Name)
            .Where(name => name != null && name.StartsWith("OpenTelemetry"))
            .OrderBy(name => name)
            .ToList();

        var json = JsonConvert.SerializeObject(otelLibs);
        var path = args[1];

        File.WriteAllText(path, json);
    }
}
