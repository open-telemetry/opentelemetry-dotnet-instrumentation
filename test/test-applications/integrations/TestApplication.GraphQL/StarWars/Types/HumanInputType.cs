using GraphQL.Types;

namespace StarWars.Types;

internal sealed class HumanInputType : InputObjectGraphType<Human>
{
    public HumanInputType()
    {
        Name = "HumanInput";
        Field<NonNullGraphType<StringGraphType>>("name");
        Field<StringGraphType>("homePlanet");
    }
}
