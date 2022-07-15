// <copyright file="ProfilerHelper.cs" company="OpenTelemetry Authors">
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

public static class ProfilerHelper
{
    public static (bool? Attached, string Message) IsProfilerAttached()
    {
        var instrumentationType = Type.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation", throwOnError: false);

        if (instrumentationType == null)
        {
            return (null, "OpenTelemetry.AutoInstrumentation.Instrumentation is not loaded");
        }

        var property = instrumentationType.GetProperty("ProfilerAttached");

        var isAttached = property?.GetValue(null) as bool?;

        return (isAttached ?? false, null);
    }
}
