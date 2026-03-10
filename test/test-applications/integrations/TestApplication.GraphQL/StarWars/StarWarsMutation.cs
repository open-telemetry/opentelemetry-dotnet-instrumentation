using GraphQL;
using GraphQL.Types;
using StarWars.Types;

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
/// }.
/// </example>
internal sealed class StarWarsMutation : ObjectGraphType
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
