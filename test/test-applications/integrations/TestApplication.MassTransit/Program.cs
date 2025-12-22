// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using MassTransit;
using TestApplication.Shared;

namespace TestApplication.MassTransit;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await CreateHostBuilder(args).Build().RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddMassTransit(x =>
                {
                    x.SetKebabCaseEndpointNameFormatter();

                    x.SetInMemorySagaRepositoryProvider();

                    var entryAssembly = Assembly.GetEntryAssembly();

                    x.AddConsumers(entryAssembly);
                    x.AddSagaStateMachines(entryAssembly);
                    x.AddSagas(entryAssembly);
                    x.AddActivities(entryAssembly);

                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                });
                services.AddHostedService<Worker>();
            });
}
