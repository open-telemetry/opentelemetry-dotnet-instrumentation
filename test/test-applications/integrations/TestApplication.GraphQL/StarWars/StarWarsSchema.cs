using GraphQL.Instrumentation;
using GraphQL.Types;

namespace StarWars;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
internal sealed class StarWarsSchema : Schema
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
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
