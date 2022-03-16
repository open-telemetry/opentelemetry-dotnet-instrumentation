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

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GraphQL.Resolvers;
using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Subscription;
using GraphQL.Types;

namespace Samples.GraphQL.StarWarsExtensions;

/// <example>
/// This is an example JSON request for a subscription
/// {
///   "query": "subscription HumanAddedSub{ humanAdded { name } }",
/// }
/// </example>
public class StarWarsSubscription : ObjectGraphType<object>
{
    private readonly StarWarsData _starWarsData;

    private readonly ISubject<Human> _humanStream = new ReplaySubject<Human>(1);

    public StarWarsSubscription(StarWarsData data)
    {
        Name = "Subscription";
        _starWarsData = data;

        AddField(new EventStreamFieldType
        {
            Name = "humanAdded",
            Type = typeof(HumanType),
            Resolver = new FuncFieldResolver<Human>(ResolveMessage),
            Subscriber = new EventStreamResolver<Human>(Subscribe)
        });
        AddField(new EventStreamFieldType
        {
            Name = "throwNotImplementedException",
            Type = typeof(HumanType),
            Resolver = new FuncFieldResolver<Human>(ResolveMessage),
            Subscriber = new EventStreamResolver<Human>(ThrowNotImplementedException)
        });
    }

    private Human ResolveMessage(ResolveFieldContext context)
    {
        return context.Source as Human;
    }

    private IObservable<Human> Subscribe(ResolveEventStreamContext context)
    {
        List<Human> listOfHumans = new List<Human>();

        var task = _starWarsData.GetHumanByIdAsync("1");
        task.Wait();
        var result = task.Result;
        if (result != null)
        {
            listOfHumans.Add(task.Result);
        }

        task = _starWarsData.GetHumanByIdAsync("2");
        task.Wait();
        result = task.Result;
        if (result != null)
        {
            listOfHumans.Add(task.Result);
        }

        return listOfHumans.ToObservable();
    }

    private IObservable<Human> ThrowNotImplementedException(ResolveEventStreamContext context)
    {
        throw new NotImplementedException("This API purposely throws a NotImplementedException");
    }
}