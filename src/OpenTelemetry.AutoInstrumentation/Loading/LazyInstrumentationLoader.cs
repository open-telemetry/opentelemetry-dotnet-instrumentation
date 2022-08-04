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

#if NETCOREAPP3_1_OR_GREATER
using System;
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

        public OnAssemblyLoadInitializer(ILifespanManager lifespanManager, InstrumentationInitializer instrumentationInitializer)
        {
            _instrumentationInitializer = instrumentationInitializer;
            _lifespanManager = lifespanManager;
            _requiredAssemblyName = instrumentationInitializer.RequiredAssemblyName;
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var assemblyName = args.LoadedAssembly.FullName.Split(new[] { ',' }, count: 2)[0];

            if (_requiredAssemblyName == assemblyName)
            {
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

                AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;
            }
        }
    }
}
#endif
