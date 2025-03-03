// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using Examples.AspNetCoreMvc.Logic;
using Examples.AspNetCoreMvc.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace Examples.AspNetCoreMvc;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseNServiceBus(context =>
            {
                var endpointConfiguration = new EndpointConfiguration("Examples.AspNetCoreMvc");
                endpointConfiguration.UseTransport<LearningTransport>();
                endpointConfiguration.UseSerialization<SystemJsonSerializer>();
                var routing = endpointConfiguration.UseTransport<LearningTransport>().Routing();
                routing.RouteToEndpoint(typeof(TestMessage), "Examples.AspNetCoreMvc");
                return endpointConfiguration;
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<BusinessLogic>();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
