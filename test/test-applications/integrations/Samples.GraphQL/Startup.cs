// <copyright file="Startup.cs" company="OpenTelemetry Authors">
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

using GraphQL;
using GraphQL.Http;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Samples.GraphQL;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDependencyResolver>(s => new FuncDependencyResolver(s.GetRequiredService));

        services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
        services.AddSingleton<IDocumentWriter, DocumentWriter>();

        services.AddSingleton<StarWarsData>();
        services.AddSingleton<StarWarsQuery>();
        services.AddSingleton<StarWarsMutation>();
        services.AddSingleton<StarWarsExtensions.StarWarsSubscription>();
        services.AddSingleton<HumanType>();
        services.AddSingleton<HumanInputType>();
        services.AddSingleton<DroidType>();
        services.AddSingleton<CharacterInterface>();
        services.AddSingleton<EpisodeEnum>();
        services.AddSingleton<ISchema, StarWarsSchema>();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddLogging(builder => builder.AddConsole());

        services.AddGraphQL(_ =>
            {
                _.EnableMetrics = true;
                _.ExposeExceptions = true;
            })
            .AddUserContextBuilder(httpContext => new GraphQLUserContext { User = httpContext.User });
    }

    public void Configure(IApplicationBuilder app,
#if NETCOREAPP2_1 || NET462
        IHostingEnvironment env,
#else
                              IWebHostEnvironment env,
#endif
        ILoggerFactory loggerFactory)
    {
        // Get StarWarsSchema Singleton
        var starWarsSchema = (StarWarsSchema)app.ApplicationServices.GetService(typeof(ISchema));

        // Get StarWarsSubscription Singleton
        var starWarsSubscription = (StarWarsExtensions.StarWarsSubscription) app.ApplicationServices.GetService(typeof(StarWarsExtensions.StarWarsSubscription));

        // Set the subscription
        // We do this roundabout mechanism to keep using the GraphQL.StarWars NuGet package
        starWarsSchema.Subscription = starWarsSubscription;
        app.UseDeveloperExceptionPage();
        app.UseWelcomePage("/alive-check");

        // add http for Schema at default url /graphql
        app.UseGraphQL<ISchema>("/graphql");

        // use graphql-playground at default url /ui/playground
        app.UseGraphQLPlayground(new GraphQLPlaygroundOptions
        {
            Path = "/ui/playground"
        });
    }
}
