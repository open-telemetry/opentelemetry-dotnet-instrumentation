using System.Reactive.Linq;
using GraphQL;
using GraphQL.Types;
using StarWars.Types;

namespace StarWars;

/// <example>
/// This is an example JSON request for a subscription
/// {
///   "query": "subscription HumanAddedSub{ humanAdded { name } }",
/// }.
/// </example>
internal sealed class StarWarsSubscription : ObjectGraphType<object>
{
    private readonly StarWarsData _starWarsData;

    public StarWarsSubscription(StarWarsData data)
    {
        Name = "Subscription";
        _starWarsData = data;

        Field<HumanType>("humanAdded")
            .ResolveStreamAsync(SubscribeAsync);

        Field<HumanType>("throwNotImplementedException")
            .ResolveStream(ThrowNotImplementedException);
    }

    private async Task<IObservable<object?>> SubscribeAsync(IResolveFieldContext<object> context)
    {
        var listOfHumans = new List<Human>();

        var result = await _starWarsData.GetHumanByIdAsync("1").ConfigureAwait(false);
        if (result != null)
        {
            listOfHumans.Add(result);
        }

        result = await _starWarsData.GetHumanByIdAsync("2").ConfigureAwait(false);
        if (result != null)
        {
            listOfHumans.Add(result);
        }

        return listOfHumans.ToObservable();
    }

    private IObservable<Human> ThrowNotImplementedException(IResolveFieldContext<object> context)
    {
        throw new NotImplementedException("This API purposely throws a NotImplementedException");
    }
}
