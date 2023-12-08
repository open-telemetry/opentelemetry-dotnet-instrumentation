// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using GraphQL;
using GraphQL.Types;
using StarWars;

namespace TestApplication.GraphQL;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<StarWarsData>();
        services.AddGraphQL(b => b
            .AddSystemTextJson()
            .AddErrorInfoProvider(opt => opt.ExposeExceptionDetails = true)
            .AddSchema<StarWarsSchema>()
            .AddGraphTypes(typeof(StarWarsSchema).Assembly));
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseWebSockets();
        app.UseGraphQL<ISchema>();
        app.UseGraphQLPlayground();
        app.UseWelcomePage("/alive-check");
    }
}
