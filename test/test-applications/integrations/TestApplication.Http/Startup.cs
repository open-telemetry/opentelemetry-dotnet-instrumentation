// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace TestApplication.Http;

public class Startup
{
    private static readonly ActivitySource MyActivitySource = new ActivitySource("TestApplication.Http", "1.0.0");

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app
#if NET8_0_OR_GREATER
            .UseRouting() // enables metrics for Microsoft.AspNetCore.Routing in .NET8+
            .UseExceptionHandler(new ExceptionHandlerOptions { ExceptionHandler = _ => Task.CompletedTask }) // together with call to /exception enables metrics for Microsoft.AspNetCore.Diagnostics for .NET8+
            .UseRateLimiter() // enables metrics for Microsoft.AspNetCore.RateLimiting in .NET8+
            .UseEndpoints(x => x.MapHub<TestHub>("/signalr")) // together with connection to SignalR Hub enables metrics for Microsoft.AspNetCore.Http.Connections for .NET8
#endif
            .Map(
                "/test",
                configuration => configuration.Run(async context =>
                {
                    using (var activity = MyActivitySource.StartActivity("manual span"))
                    {
                        activity?.SetTag("test_tag", "test_value");
                    }

                    await context.Response.WriteAsync("Pong");
                }))
            .Map(
                "/exception",
                configuration => configuration.Run(_ => throw new InvalidOperationException("Just to throw something")));
    }
}
