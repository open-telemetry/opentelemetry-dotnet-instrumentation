// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.Worker;

public class Worker : BackgroundService
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
        _logger.LogInformation("Worker running.");
        await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
        // When completed, the entire app host will stop.
        _hostApplicationLifetime.StopApplication();
    }
}
