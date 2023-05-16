// <copyright file="StarWarsMutation.cs" company="OpenTelemetry Authors">
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
using StarWars.Types;
using TestApplication.GraphQL.NativeSupport.StarWars.Types;

namespace StarWars;

/// <example>
/// This is an example JSON request for a mutation
/// {
///   "query": "mutation ($human:HumanInput!){ createHuman(human: $human) { id name } }",
///   "variables": {
///     "human": {
///       "name": "Boba Fett"
///     }
///   }
/// }
/// </example>
public class StarWarsMutation : ObjectGraphType
{
    public StarWarsMutation(StarWarsData data)
    {
        Name = "Mutation";

        Field<HumanType>("createHuman")
            .Argument<NonNullGraphType<HumanInputType>>("human")
            .Resolve(context =>
            {
                var human = context.GetArgument<Human>("human");
                return data.AddCharacter(human);
            });
    }
}
