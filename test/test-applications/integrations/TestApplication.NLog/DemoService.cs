// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using NLog;

namespace TestApplication.NLog;

/// <summary>
/// Demo service that demonstrates logging from within dependency injection container.
/// </summary>
public class DemoService : IDemoService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly Microsoft.Extensions.Logging.ILogger<DemoService> _msLogger;

    public DemoService(Microsoft.Extensions.Logging.ILogger<DemoService> msLogger)
    {
        _msLogger = msLogger;
    }

    /// <summary>
    /// Demonstrates various logging scenarios within a service.
    /// </summary>
    public async Task DemonstrateServiceLoggingAsync()
    {
        Logger.Info("Starting service demonstration");

        // Simulate async work with logging
        await SimulateAsyncWorkAsync();

        // Test both NLog and Microsoft.Extensions.Logging
        Logger.Debug("Service work completed using NLog");
        _msLogger.LogDebug("Service work completed using Microsoft.Extensions.Logging");

        Logger.Info("Service demonstration completed");
    }

    /// <summary>
    /// Simulates asynchronous work with progress logging.
    /// </summary>
    private async Task SimulateAsyncWorkAsync()
    {
        const int totalSteps = 5;

        for (int i = 1; i <= totalSteps; i++)
        {
            Logger.Debug("Processing step {Step} of {TotalSteps}", i, totalSteps);

            // Simulate work
            await Task.Delay(100);

            if (i == 3)
            {
                Logger.Warn("Step {Step} took longer than expected", i);
            }
        }

        Logger.Info("Async work completed successfully after {TotalSteps} steps", totalSteps);
    }
}
