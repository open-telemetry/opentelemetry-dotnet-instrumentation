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
using System.Threading;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Loading;

internal class LazyInstrumentationLoader : ILifespanManager, IDisposable
{
    // some instrumentations requires to keep references to objects
    // so that they are not garbage collected
    private readonly ConcurrentBag<object> _instrumentations = new();

    public void Dispose()
    {
        while (_instrumentations.TryTake(out var instrumentation))
        {
            if (instrumentation is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public void Add(InstrumentationInitializer loader)
    {
        _ = new OnAssemblyLoadInitializer(this, loader);
    }

    void ILifespanManager.Track(object instance)
    {
        _instrumentations.Add(instance);
    }

    private class OnAssemblyLoadInitializer
    {
        private static readonly ILogger Logger = OtelLogging.GetLogger();
        private readonly InstrumentationInitializer _instrumentationInitializer;
        private readonly LazyInstrumentationLoader _manager;
        private readonly HashSet<string> _requiredAssemblies;
        private int _loadedCount;

        public OnAssemblyLoadInitializer(LazyInstrumentationLoader manager, InstrumentationInitializer instrumentationInitializer)
        {
            _instrumentationInitializer = instrumentationInitializer;
            _manager = manager;
            _requiredAssemblies = new HashSet<string>(instrumentationInitializer.RequiredAssemblies);
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var assemblyName = args.LoadedAssembly.FullName.Split(new[] { ',' }, count: 2)[0];

            if (_requiredAssemblies.Contains(assemblyName))
            {
                if (Interlocked.Increment(ref _loadedCount) == _requiredAssemblies.Count)
                {
                    var initializerName = _instrumentationInitializer.GetType().Name;
                    Logger.Debug("'{0}' started", initializerName);

                    try
                    {
                        _instrumentationInitializer.Initialize(_manager);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "'{0}' failed", initializerName);
                    }

                    AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;
                }
            }
        }
    }
}
