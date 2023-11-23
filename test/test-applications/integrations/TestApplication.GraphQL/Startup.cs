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
