// <copyright file="AspNetCoreDetector.cs" company="OpenTelemetry Authors">
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

#if NETCOREAPP

using System;
using System.Collections.Generic;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Loading
{
    internal class AspNetCoreDetector : AssemblyLoadDetector
    {
        private static readonly IEnumerable<string> _requiredAssemblies = new string[]
        {
            "Microsoft.AspNetCore"
        };

        public AspNetCoreDetector()
            : base(_requiredAssemblies)
        {
        }

        internal override Action<TracerProvider> GetInstrumentationLoader()
        {
            return (TracerProvider provider) =>
            {
                var instrumentationType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreInstrumentation, OpenTelemetry.Instrumentation.AspNetCore");
                var httpInListenerType = Type.GetType("OpenTelemetry.Instrumentation.AspNetCore.Implementation.HttpInListener, OpenTelemetry.Instrumentation.AspNetCore");

                object httpInListener = Activator.CreateInstance(httpInListenerType, args: new OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreInstrumentationOptions());
                object instrumentation = Activator.CreateInstance(instrumentationType, args: httpInListener);

                provider.AddInstrumentation(instrumentation);
            };
        }
    }
}

#endif
