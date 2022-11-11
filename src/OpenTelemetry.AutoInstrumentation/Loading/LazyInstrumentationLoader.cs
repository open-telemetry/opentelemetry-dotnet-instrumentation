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
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Loading;

/// <summary>
/// LazyInstrumentationLoader is responsible for managing deferred instrumentation initialization.
/// </summary>
/// <remarks>
/// Some instrumentations require setup that can be preformed
/// when some prerequisites are met.
/// </remarks>
internal class LazyInstrumentationLoader : IDisposable
{
    public ILifespanManager LifespanManager { get; } = new InstrumentationLifespanManager();

    public void Dispose()
    {
        LifespanManager.Dispose();
    }

    public void Add(InstrumentationInitializer loader)
    {
        _ = new OnAssemblyLoadInitializer(LifespanManager, loader);
    }

    private class OnAssemblyLoadInitializer
    {
        private static readonly ILogger Logger = OtelLogging.GetLogger();
        private readonly InstrumentationInitializer _instrumentationInitializer;
        private readonly ILifespanManager _lifespanManager;
        private readonly string _requiredAssemblyName;

#if NETFRAMEWORK
        private int _initialized;
#endif

        public OnAssemblyLoadInitializer(ILifespanManager lifespanManager, InstrumentationInitializer instrumentationInitializer)
        {
            _instrumentationInitializer = instrumentationInitializer;
            _lifespanManager = lifespanManager;
            _requiredAssemblyName = instrumentationInitializer.RequiredAssemblyName;

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;

#if NETFRAMEWORK

            // 1. NetFramework doesn't have startuphook that executes before the main content.
            //    So, we must check at the startup, if the required assembly is already loaded.
            // 2. There are multiple race conditions here between assembly loaded event and checking
            //    if the required assembly is already loaded.
            // 3. To eliminate risks that initializer doesn't invoke, we ensure that both strategies
            //    are active at the same time, whichever executes first, determines the loading moment.

            var isRequiredAssemblyLoaded = AppDomain.CurrentDomain
                .GetAssemblies()
                .Any(x => GetAssemblyName(x) == _requiredAssemblyName);
            if (isRequiredAssemblyLoaded)
            {
                OnRequiredAssemblyDetected();
            }
#endif
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var assemblyName = GetAssemblyName(args.LoadedAssembly);

            if (_requiredAssemblyName == assemblyName)
            {
                OnRequiredAssemblyDetected();
            }
        }

        private void OnRequiredAssemblyDetected()
        {
#if NETFRAMEWORK
            if (Interlocked.Exchange(ref _initialized, value: 1) != default)
            {
                // OnRequiredAssemblyDetected() was already called before
                return;
            }
#endif

            AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;

            var initializerName = _instrumentationInitializer.GetType().Name;
            Logger.Debug("'{0}' started", initializerName);

            try
            {
                _instrumentationInitializer.Initialize(_lifespanManager);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "'{0}' failed", initializerName);
            }
        }

        private string GetAssemblyName(Assembly assembly)
        {
            return assembly.FullName.Split(new[] { ',' }, count: 2)[0];
        }
    }
}
