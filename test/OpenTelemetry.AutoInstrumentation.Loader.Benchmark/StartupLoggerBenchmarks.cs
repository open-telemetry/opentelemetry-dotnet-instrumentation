// <copyright file="StartupLoggerBenchmarks.cs" company="OpenTelemetry Authors">
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

using BenchmarkDotNet.Attributes;

/*
BenchmarkDotNet=v0.13.4, OS=Windows 11 (10.0.22621.1105)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=7.0.200-preview.22628.1
  [Host]     : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2


|                Method |     Mean |     Error |    StdDev |
|---------------------- |---------:|----------:|----------:|
| SetStartupLogFilePath | 1.460 us | 0.0077 us | 0.0068 us |
 */

namespace OpenTelemetry.AutoInstrumentation.Loader.Benchmark;
public class StartupLoggerBenchmarks
{
    [Benchmark]
    public void SetStartupLogFilePath()
    {
        var path = StartupLogger.SetStartupLogFilePath();
    }
}
