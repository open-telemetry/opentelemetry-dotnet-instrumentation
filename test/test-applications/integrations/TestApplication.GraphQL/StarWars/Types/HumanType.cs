using GraphQL.Types;
using StarWars.Extensions;

namespace StarWars.Types;

public class HumanType : ObjectGraphType<Human>
{
    public HumanType(StarWarsData data)
    {
        Name = "Human";

        Field<NonNullGraphType<StringGraphType>>("id")
            .Description("The id of the human.")
            .Resolve(context => context.Source.Id);

        Field<StringGraphType>("name")
            .Description("The name of the human.")
            .Resolve(context => context.Source.Name);

        Field<ListGraphType<CharacterInterface>>("friends")
            .Resolve(context => data.GetFriends(context.Source));

#if GRAPHQL_7_7_OR_GREATER
        Connection<CharacterInterface>("friendsConnection")
#else
        Connection<CharacterInterface>()
            .Name("friendsConnection")
#endif
            .Description("A list of a character's friends.")
            .Bidirectional()
            .Resolve(context => context.GetPagedResults<Human, StarWarsCharacter>(data, context.Source.Friends));

        Field<ListGraphType<Episodes>>("appearsIn").Description("Which movie they appear in.");

        Field<StringGraphType>("homePlanet")
            .Description("The home planet of the human.")
            .Resolve(context => context.Source.HomePlanet);

        Interface<CharacterInterface>();
    }
}
