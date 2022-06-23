// <copyright file="OutputHelper.cs" company="OpenTelemetry Authors">
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
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public static class OutputHelper
{
    public static void WriteResult(this ITestOutputHelper outputHelper, ProcessHelper processHelper)
    {
        processHelper.Drain();

        string standardOutput = processHelper.StandardOutput;
        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            outputHelper.WriteLine($"StandardOutput:{Environment.NewLine}{standardOutput}");
        }

        string standardError = processHelper.ErrorOutput;
        if (!string.IsNullOrWhiteSpace(standardError))
        {
            outputHelper.WriteLine($"StandardError:{Environment.NewLine}{standardError}");
        }
    }
}
