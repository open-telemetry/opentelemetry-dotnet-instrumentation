// <copyright file="Logger.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper;

internal static class Logger
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    internal static void LogInformation(string message)
    {
        Log.Information(message);
        BootstrapperEventSource.Log.Trace(message);
    }

    internal static void LogError(string message)
    {
        Log.Error(message);
        BootstrapperEventSource.Log.Error(message);
    }
}
