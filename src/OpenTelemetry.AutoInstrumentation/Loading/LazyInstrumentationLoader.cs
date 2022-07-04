// <copyright file="LazyInstrumentationLoader.cs" company="OpenTelemetry Authors">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Loading
{
    internal class LazyInstrumentationLoader : IDisposable
    {
        private static readonly ILogger Logger = OtelLogging.GetLogger();

        private readonly List<AssemblyLoadDetector> _assemblyLoadDetectors = new();
        private readonly ConcurrentBag<object> _instrumentations = new();

        public LazyInstrumentationLoader(LazyInstrumentationBuilders subscriptions)
        {
            foreach (var builder in subscriptions.EnabledBuilders)
            {
                var detector = builder();
                detector.OnReady += Detector_OnReady;

                _assemblyLoadDetectors.Add(detector);
            }
        }

        public IEnumerable<object> Instrumentations => _instrumentations;

        public void Dispose()
        {
            while (_instrumentations.TryTake(out object instrumentation))
            {
                if (instrumentation is IDisposable disposableInstrumentation)
                {
                    disposableInstrumentation.Dispose();
                }
            }
        }

        private void Detector_OnReady(object sender, AssemblyLoadDetector.LoadDetectorReadyEventArgs e)
        {
            string detectorName = sender.GetType().Name;

            Logger.Debug("Detector '{0}' executed load", detectorName);

            try
            {
                var instrumentation = e.Builder();

                // We just track the instances to keep them alive.
                // During the shut down event IDisposable types are also disposed.
                // This behavior repliactes SDK internal logic.
                _instrumentations.Add(instrumentation);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not execute '{0}' load", detectorName);
            }

            var detector = (AssemblyLoadDetector)sender;
            detector.OnReady -= Detector_OnReady;

            _assemblyLoadDetectors.Remove(detector);

            Logger.Debug("Detector '{0}' removed", detectorName);
        }
    }
}
