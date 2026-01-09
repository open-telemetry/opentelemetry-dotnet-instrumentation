// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.Worker;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This is instantiated by the Host builder.
internal sealed partial class Worker : BackgroundService
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This is instantiated by the Host builder.
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public Worker(ILogger<Worker> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerRunning();
        await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
        // When completed, the entire app host will stop.
        _hostApplicationLifetime.StopApplication();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker running.")]
    private partial void LogWorkerRunning();
}
