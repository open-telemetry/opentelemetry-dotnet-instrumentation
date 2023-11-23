using GraphQL.Instrumentation;
using GraphQL.Types;

namespace StarWars;

public class StarWarsSchema : Schema
{
    public StarWarsSchema(IServiceProvider provider)
        : base(provider)
    {
        Query = provider.GetService(typeof(StarWarsQuery)) as StarWarsQuery ?? throw new InvalidOperationException();
        Mutation = provider.GetService(typeof(StarWarsMutation)) as StarWarsMutation ?? throw new InvalidOperationException();
        Subscription = provider.GetService(typeof(StarWarsSubscription)) as StarWarsSubscription ?? throw new InvalidOperationException();

        FieldMiddleware.Use(new InstrumentFieldsMiddleware());
    }
}
