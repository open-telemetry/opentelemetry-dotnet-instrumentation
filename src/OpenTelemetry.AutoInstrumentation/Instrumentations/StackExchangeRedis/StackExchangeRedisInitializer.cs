// <copyright file="StackExchangeRedisInitializer.cs" company="OpenTelemetry Authors">
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

#if NETCOREAPP3_1_OR_GREATER

using System;
using OpenTelemetry.AutoInstrumentation.Configuration;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis;

internal static class StackExchangeRedisInitializer
{
    public static void Initialize(object connection)
    {
        if (connection != null && Instrumentation.TracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.StackExchangeRedis))
        {
            var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisCallsInstrumentation, OpenTelemetry.Instrumentation.StackExchangeRedis");
            var optionsInstrumentationType = Type.GetType("OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisCallsInstrumentationOptions, OpenTelemetry.Instrumentation.StackExchangeRedis");

            var options = Activator.CreateInstance(optionsInstrumentationType);
            var instrumentation = Activator.CreateInstance(instrumentationType, connection, options);

            Instrumentation.LifespanManager.Track(instrumentation);
        }
    }
}
#endif
