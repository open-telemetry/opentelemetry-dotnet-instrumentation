// <copyright file="ConsoleHelper.cs" company="OpenTelemetry Authors">
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

namespace TestApplication.Shared;

public static class ConsoleHelper
{
    public static void WriteSplashScreen(string[] args)
    {
        Console.WriteLine($"Command line: {string.Join(" ", args)}");
        Console.WriteLine($"Profiler attached: {ProfilerHelper.IsProfilerAttached()}");
        Console.WriteLine($"Platform: {(Environment.Is64BitProcess ? "x64" : "x32")}");
    }
}
