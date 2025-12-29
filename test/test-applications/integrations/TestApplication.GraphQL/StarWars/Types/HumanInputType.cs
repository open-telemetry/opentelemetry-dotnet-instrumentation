using GraphQL.Types;

namespace StarWars.Types;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by GraphQL.
internal sealed class HumanInputType : InputObjectGraphType<Human>
#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by GraphQL.
{
    public HumanInputType()
    {
        Name = "HumanInput";
        Field<NonNullGraphType<StringGraphType>>("name");
        Field<StringGraphType>("homePlanet");
    }
}
