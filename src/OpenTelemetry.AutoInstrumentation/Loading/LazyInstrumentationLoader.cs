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
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Loading
{
    internal class LazyInstrumentationLoader
    {
        private readonly ConcurrentBag<AssemblyLoadDetector> _assemblyLoadDetectors = new ConcurrentBag<AssemblyLoadDetector>();

        private TracerProviderBuilder _builder;
        private TracerProvider _provider;

        public LazyInstrumentationLoader()
        {
#if NETCOREAPP
            SubScribe(new AspNetCoreDetector());
#endif
        }

        public void OnBuilderAvailable(TracerProviderBuilder builder)
        {
            Console.WriteLine("builder is now available");

            _builder = builder;
        }

        public void OnProviderAvailable(TracerProvider provider)
        {
            Console.WriteLine("Provider is now available");

            _provider = provider;
        }

        private void SubScribe(AssemblyLoadDetector detector)
        {
            detector.OnReady += Detector_OnReady;

            _assemblyLoadDetectors.Add(detector);
        }

        private void Detector_OnReady(object sender, AssemblyLoadDetector.LoadDetectorReadyEventArgs e)
        {
            if (_provider != null)
            {
                Console.WriteLine("Provider '{0}' executed load", sender.GetType().Name);

                try
                {
                    e.Loader(_provider);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not execute '{0}' load", sender.GetType().Name);
                    Console.WriteLine(ex);
                }

                ((AssemblyLoadDetector)sender).OnReady -= Detector_OnReady;

                // TODO: Dispose the sender, it's not needed any more
            }
        }
    }
}
