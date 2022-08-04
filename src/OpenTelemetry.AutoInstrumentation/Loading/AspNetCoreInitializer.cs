// <copyright file="AspNetCoreInitializer.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Loading
{
    internal class AspNetCoreInitializer : InstrumentationInitializer
    {
        public AspNetCoreInitializer()
            : base("Microsoft.AspNetCore.Http")
        {
        }

        public override void Initialize(ILifespanManager lifespanManager)
        {
            var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreInstrumentation, OpenTelemetry.Instrumentation.AspNetCore");
            var httpInListenerType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.Implementation.HttpInListener, OpenTelemetry.Instrumentation.AspNetCore");

            var httpInListener = Activator.CreateInstance(httpInListenerType, args: new OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreInstrumentationOptions());
            var instrumentation = Activator.CreateInstance(instrumentationType, args: httpInListener);

            lifespanManager.Track(instrumentation);
        }
    }
}
#endif
