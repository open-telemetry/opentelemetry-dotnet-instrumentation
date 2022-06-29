// <copyright file="AssemblyLoadDetector.cs" company="OpenTelemetry Authors">
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
using System.Linq;
using System.Threading;

namespace OpenTelemetry.AutoInstrumentation.Loading
{
    internal abstract class AssemblyLoadDetector
    {
        private HashSet<string> _requiredAssemblies;
        private int _loadedCount;

        public AssemblyLoadDetector(IEnumerable<string> requiredAssemblies)
        {
            _requiredAssemblies = new HashSet<string>(requiredAssemblies);

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        public event EventHandler<LoadDetectorReadyEventArgs> OnReady;

        internal abstract Action<ConcurrentBag<object>> GetInstrumentationLoader();

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            string assemblyName = args.LoadedAssembly.FullName.Split(',')[0];

            if (_requiredAssemblies.Contains(assemblyName, StringComparer.InvariantCultureIgnoreCase))
            {
                if (Interlocked.Increment(ref _loadedCount) == _requiredAssemblies.Count)
                {
                    var loader = GetInstrumentationLoader();

                    OnReady?.Invoke(this, new LoadDetectorReadyEventArgs(loader));

                    AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;
                }
            }
        }

        public class LoadDetectorReadyEventArgs : EventArgs
        {
            public LoadDetectorReadyEventArgs(Action<ConcurrentBag<object>> loader)
            {
                Loader = loader;
            }

            public Action<ConcurrentBag<object>> Loader { get; }
        }
    }
}
