using GraphQL;
using GraphQL.Types;
using StarWars.Types;

namespace StarWars;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by GraphQL.
internal sealed class StarWarsQuery : ObjectGraphType<object>
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by GraphQL.
{
    public StarWarsQuery(StarWarsData data)
    {
        Name = "Query";

        Field<CharacterInterface>("hero")
            .ResolveAsync(async context => await data.GetDroidByIdAsync("3").ConfigureAwait(false));

        Field<HumanType>("human")
            .Argument<NonNullGraphType<StringGraphType>>("id", "id of the human")
            .ResolveAsync(async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id")).ConfigureAwait(false));

        Func<IResolveFieldContext, string, Task<Droid?>> func = (context, id) => data.GetDroidByIdAsync(id);

        Field<DroidType>("droid")
            .Argument<NonNullGraphType<StringGraphType>>("id", "id of the droid")
            .ResolveDelegate(func);
    }
}
