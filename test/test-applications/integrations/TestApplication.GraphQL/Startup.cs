// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using GraphQL;
using GraphQL.Types;
using StarWars;

namespace TestApplication.GraphQL;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
internal sealed class Startup
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<StarWarsData>();
        services.AddGraphQL(b => b
            .AddSystemTextJson()
            .AddErrorInfoProvider(opt => opt.ExposeExceptionDetails = true)
            .AddSchema<StarWarsSchema>()
            .AddGraphTypes(typeof(StarWarsSchema).Assembly));
    }

    public static void Configure(IApplicationBuilder app)
    {
        app.UseMiddleware<CsrfMiddleware>();
        app.UseDeveloperExceptionPage();
        app.UseWebSockets();
        app.UseGraphQL<ISchema>();
        app.UseGraphQLGraphiQL();
        app.UseWelcomePage("/alive-check");
    }
}
