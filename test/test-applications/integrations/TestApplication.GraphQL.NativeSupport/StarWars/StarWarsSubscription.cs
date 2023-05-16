// <copyright file="StarWarsSubscription.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Reactive.Linq;
using System.Reactive.Subjects;
using GraphQL;
using GraphQL.Types;
using StarWars.Types;

namespace StarWars;

/// <example>
/// This is an example JSON request for a subscription
/// {
///   "query": "subscription HumanAddedSub{ humanAdded { name } }",
/// }
/// </example>
internal class StarWarsSubscription : ObjectGraphType<object>
{
    private readonly StarWarsData _starWarsData;

    private readonly ISubject<Human> _humanStream = new ReplaySubject<Human>(1);

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

        var result = await _starWarsData.GetHumanByIdAsync("1");
        if (result != null)
        {
            listOfHumans.Add(result);
        }

        result = await _starWarsData.GetHumanByIdAsync("2");
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
