// <copyright file="StarWarsSchema.cs" company="OpenTelemetry Authors">
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

using GraphQL.Instrumentation;
using GraphQL.Types;

namespace StarWars;

public class StarWarsSchema : Schema
{
    public StarWarsSchema(IServiceProvider provider)
        : base(provider)
    {
        Query = provider.GetService(typeof(StarWarsQuery)) as StarWarsQuery ?? throw new InvalidOperationException();
        Mutation = provider.GetService(typeof(StarWarsMutation)) as StarWarsMutation ?? throw new InvalidOperationException();
        Subscription = provider.GetService(typeof(StarWarsSubscription)) as StarWarsSubscription ?? throw new InvalidOperationException();

        FieldMiddleware.Use(new InstrumentFieldsMiddleware());
    }
}
