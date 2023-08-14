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

using System.Reflection;
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
        private static readonly IOtelLogger OtelLogger = OtelLogging.GetLogger();
        private readonly InstrumentationInitializer _instrumentationInitializer;
        private readonly ILifespanManager _lifespanManager;
        private readonly string _requiredAssemblyName;
        private int _initialized;

        public OnAssemblyLoadInitializer(ILifespanManager lifespanManager, InstrumentationInitializer instrumentationInitializer)
        {
            _instrumentationInitializer = instrumentationInitializer;
            _lifespanManager = lifespanManager;
            _requiredAssemblyName = instrumentationInitializer.RequiredAssemblyName;

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;

            // 1. NetFramework doesn't have startuphook that executes before the main content.
            //    So, we must check at the startup, if the required assembly is already loaded.
            // 2. There are multiple race conditions here between assembly loaded event and checking
            //    if the required assembly is already loaded.
            // 3. To eliminate risks that initializer doesn't invoke, we ensure that both strategies
            //    are active at the same time, whichever executes first, determines the loading moment.

            var isRequiredAssemblyLoaded = Array.Exists(AppDomain.CurrentDomain.GetAssemblies(), x => IsAssemblyNameEqual(x, _requiredAssemblyName));
            if (isRequiredAssemblyLoaded)
            {
                OnRequiredAssemblyDetected();
            }
        }

        private static bool IsAssemblyNameEqual(Assembly assembly, string expectedAssemblyName)
        {
            var assemblyName = assembly.FullName.AsSpan();
            if (assemblyName.Length <= expectedAssemblyName.Length)
            {
                return false;
            }

            return assemblyName.StartsWith(expectedAssemblyName.AsSpan()) && assemblyName[expectedAssemblyName.Length] == ',';
        }

        private void CurrentDomain_AssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            if (IsAssemblyNameEqual(args.LoadedAssembly, _requiredAssemblyName))
            {
                OnRequiredAssemblyDetected();
            }
        }

        private void OnRequiredAssemblyDetected()
        {
            if (Interlocked.Exchange(ref _initialized, value: 1) != default)
            {
                // OnRequiredAssemblyDetected() was already called before
                return;
            }

            AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;

            var initializerName = _instrumentationInitializer.GetType().Name;
            OtelLogger.Debug("'{0}' started", initializerName);

            try
            {
                _instrumentationInitializer.Initialize(_lifespanManager);
            }
            catch (Exception ex)
            {
                OtelLogger.Error(ex, "'{0}' failed", initializerName);
            }
        }
    }
}
