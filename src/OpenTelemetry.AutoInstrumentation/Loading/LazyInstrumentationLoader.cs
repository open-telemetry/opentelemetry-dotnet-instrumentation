// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
            if (Interlocked.Exchange(ref _initialized, value: 1) != 0)
            {
                // OnRequiredAssemblyDetected() was already called before
                return;
            }

            AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;

            var initializerName = _instrumentationInitializer.InitializerName;
            OtelLogger.Debug("Starting '{0}' initializer", initializerName);

            var noExceptions = true;

            try
            {
                _instrumentationInitializer.Initialize(_lifespanManager);
            }
            catch (Exception ex)
            {
                noExceptions = false;
                OtelLogger.Error(ex, "'{0}' failed", initializerName);
            }

            if (noExceptions)
            {
                OtelLogger.Debug("Initializer '{0}' completed", initializerName);
            }
        }
    }
}
