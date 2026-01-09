using GraphQL.Types;
using GraphQL.Types.Relay;

namespace StarWars.Types;

internal sealed class CharacterInterface : InterfaceGraphType<StarWarsCharacter>
{
    public CharacterInterface()
    {
        Name = "Character";

        Field<NonNullGraphType<StringGraphType>>("id")
            .Description("The id of the character.");

        Field<StringGraphType>("name")
            .Description("The name of the character.");

        Field<ListGraphType<CharacterInterface>>("friends");
        Field<ConnectionType<CharacterInterface, EdgeType<CharacterInterface>>>("friendsConnection");

        Field<ListGraphType<Episodes>>("appearsIn")
            .Description("Which movie they appear in.");
    }
}
