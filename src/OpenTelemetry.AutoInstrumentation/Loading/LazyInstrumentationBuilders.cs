// <copyright file="LazyInstrumentationBuilders.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using OpenTelemetry.AutoInstrumentation.Configuration;

namespace OpenTelemetry.AutoInstrumentation.Loading
{
    internal class LazyInstrumentationBuilders
    {
        // Add here all of the available detectors
        private static readonly IReadOnlyDictionary<TracerInstrumentation, Func<AssemblyLoadDetector>> _defaultDetectors
            = new Dictionary<TracerInstrumentation, Func<AssemblyLoadDetector>>()
        {
            { TracerInstrumentation.AspNet, () => new AspNetCoreDetector() }
        };

        private readonly List<Func<AssemblyLoadDetector>> _assemblyLoadDetectors = new();
        private readonly TracerSettings _tracerSettings;

        public LazyInstrumentationBuilders(TracerSettings tracerSettings)
            : this(tracerSettings, _defaultDetectors)
        {
        }

        internal LazyInstrumentationBuilders(TracerSettings tracerSettings, IReadOnlyDictionary<TracerInstrumentation, Func<AssemblyLoadDetector>> detectors)
        {
            _tracerSettings = tracerSettings;

            foreach (var item in detectors)
            {
                AddBuilder(item.Key, item.Value);
            }
        }

        public IReadOnlyCollection<Func<AssemblyLoadDetector>> EnabledBuilders => _assemblyLoadDetectors;

        private void AddBuilder(TracerInstrumentation instrumentation, Func<AssemblyLoadDetector> builder)
        {
            if (_tracerSettings.EnabledInstrumentations.Contains(instrumentation))
            {
                _assemblyLoadDetectors.Add(builder);
            }
        }
    }
}
