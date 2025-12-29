using GraphQL.Types;
using StarWars.Extensions;

namespace StarWars.Types;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by GraphQL.
internal sealed class DroidType : ObjectGraphType<Droid>
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by GraphQL.
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

#if GRAPHQL_7_7_OR_GREATER
        Connection<CharacterInterface>("friendsConnection")
#else
        Connection<CharacterInterface>()
            .Name("friendsConnection")
#endif
            .Description("A list of a character's friends.")
            .Bidirectional()
            .Resolve(context => context.GetPagedResults<Droid, StarWarsCharacter>(data, context.Source.Friends));

        Field<ListGraphType<Episodes>>("appearsIn")
            .Description("Which movie they appear in.");

        Field(d => d.PrimaryFunction, nullable: true)
            .Description("The primary function of the droid.");

        Interface<CharacterInterface>();
    }
}
