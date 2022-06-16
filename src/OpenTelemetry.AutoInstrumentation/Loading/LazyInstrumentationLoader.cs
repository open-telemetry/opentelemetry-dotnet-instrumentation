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
using System.Collections.Generic;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Loading
{
    internal class LazyInstrumentationLoader
    {
        private readonly List<AssemblyLoadDetector> _assemblyLoadDetectors = new List<AssemblyLoadDetector>();

        private TracerProvider _provider;
        private TracerSettings _tracerSettings;

        public LazyInstrumentationLoader(TracerSettings tracerSettings)
        {
            _tracerSettings = tracerSettings;

#if NETCOREAPP
            SubScribe(TracerInstrumentation.AspNet, () => new AspNetCoreDetector());
#endif
        }

        public void OnProviderAvailable(TracerProvider provider)
        {
            Console.WriteLine("Provider is now available");

            _provider = provider;
        }

        private void SubScribe(TracerInstrumentation instrumentation, Func<AssemblyLoadDetector> builder)
        {
            if (_tracerSettings.EnabledInstrumentations.Contains(instrumentation))
            {
                var detector = builder();
                detector.OnReady += Detector_OnReady;

                _assemblyLoadDetectors.Add(detector);
            }
        }

        private void Detector_OnReady(object sender, AssemblyLoadDetector.LoadDetectorReadyEventArgs e)
        {
            if (_provider != null)
            {
                Console.WriteLine("Detector '{0}' executed load", sender.GetType().Name);

                try
                {
                    e.Loader(_provider);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not execute '{0}' load", sender.GetType().Name);
                    Console.WriteLine(ex);
                }

                var detector = (AssemblyLoadDetector)sender;
                detector.OnReady -= Detector_OnReady;

                _assemblyLoadDetectors.Remove(detector);

                Console.WriteLine("Detector '{0}' removed", sender.GetType().Name);
            }
        }
    }
}
