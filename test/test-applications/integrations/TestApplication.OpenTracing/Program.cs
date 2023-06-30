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

using OpenTracing.Util;
using TestApplication.Shared;

namespace TestApplication.MySqlData;

public static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        // Simulate typical usage of OpenTracing. Notice that the automatic instrumentation
        // must register the global tracer instance before any reference to OpenTracing.Util.GlobalTracer.Instance.
        var otTracer = GlobalTracer.Instance;
        using var otScopeManager = otTracer.BuildSpan("MySpan").StartActive();
        var otSpan = otScopeManager.Span;
        otSpan.SetTag("MyTag", "MyValue");
    }
}
