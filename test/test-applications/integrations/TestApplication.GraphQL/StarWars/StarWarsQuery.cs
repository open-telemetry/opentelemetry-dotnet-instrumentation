using GraphQL;
using GraphQL.Types;
using StarWars.Types;

namespace StarWars;

public class StarWarsQuery : ObjectGraphType<object>
{
    public StarWarsQuery(StarWarsData data)
    {
        Name = "Query";

        Field<CharacterInterface>("hero")
            .ResolveAsync(async context => await data.GetDroidByIdAsync("3"));

        Field<HumanType>("human")
            .Argument<NonNullGraphType<StringGraphType>>("id", "id of the human")
            .ResolveAsync(async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id")));

        Func<IResolveFieldContext, string, Task<Droid?>> func = (context, id) => data.GetDroidByIdAsync(id);

        Field<DroidType>("droid")
            .Argument<NonNullGraphType<StringGraphType>>("id", "id of the droid")
            .ResolveDelegate(func);
    }
}
