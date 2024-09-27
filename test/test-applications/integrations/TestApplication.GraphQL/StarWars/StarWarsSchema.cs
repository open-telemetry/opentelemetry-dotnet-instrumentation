using GraphQL.Instrumentation;
using GraphQL.Types;

namespace StarWars;

public class StarWarsSchema : Schema
{
    public StarWarsSchema(IServiceProvider provider)
        : base(provider)
    {
        Query = provider.GetRequiredService<StarWarsQuery>();
        Mutation = provider.GetRequiredService<StarWarsMutation>();
        Subscription = provider.GetRequiredService<StarWarsSubscription>();

        FieldMiddleware.Use(new InstrumentFieldsMiddleware());
    }
}
