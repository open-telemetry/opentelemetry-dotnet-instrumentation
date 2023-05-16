// <copyright file="DroidType.cs" company="OpenTelemetry Authors">
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

using GraphQL.Types;
using StarWars.Extensions;

namespace StarWars.Types;

public class DroidType : ObjectGraphType<Droid>
{
    public DroidType(StarWarsData data)
    {
        Name = "Droid";
        Description = "A mechanical creature in the Star Wars universe.";

        Field<NonNullGraphType<StringGraphType>>("id")
            .Description("The id of the droid.")
            .Resolve(context => context.Source.Id);

        Field<StringGraphType>("name")
            .Description("The name of the droid.")
            .Resolve(context => context.Source.Name);

        Field<ListGraphType<CharacterInterface>>("friends")
            .Resolve(context => data.GetFriends(context.Source));

        Connection<CharacterInterface>()
            .Name("friendsConnection")
            .Description("A list of a character's friends.")
            .Bidirectional()
            .Resolve(context => context.GetPagedResults<Droid, StarWarsCharacter>(data, context.Source.Friends));

        Field<ListGraphType<EpisodeEnum>>("appearsIn")
            .Description("Which movie they appear in.");

        Field(d => d.PrimaryFunction, nullable: true)
            .Description("The primary function of the droid.");

        Interface<CharacterInterface>();
    }
}
