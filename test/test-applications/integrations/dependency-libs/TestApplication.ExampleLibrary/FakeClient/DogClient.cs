// <copyright file="DogClient.cs" company="OpenTelemetry Authors">
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
using System.Threading.Tasks;

namespace TestApplication.ExampleLibrary.FakeClient;

public class DogClient<T1, T2>
{
    public void Silence()
    {
        Task.Delay(1).Wait();
    }

    public string TellMeIfTheCookieIsYummy(Biscuit.Cookie cookie, Biscuit.Cookie.Raisin raisin)
    {
        if (cookie.IsYummy)
        {
            if (raisin.IsPurple)
            {
                return "Yes, it is yummy, with purple raisins.";
            }

            return "Yes, it is yummy, with white raisins.";
        }

        return "No, it is not yummy";
    }

    public void Sit(
        string message,
        int howManyTimes,
        byte[] whatEvenIs = null,
        Guid[][] whatEvenIsThis = null,
        T1[][][] whatEvenIsThisT = null,
        List<byte[][]> evenMoreWhatIsThis = null,
        List<DogTrick<T1>> previousTricks = null,
        Tuple<int, T1, string, object, Tuple<Tuple<T2, long>, long>, Task, Guid> tuple = null,
        Dictionary<int, IList<Task<DogTrick<T1>>>> whatAmIDoing = null)
    {
        for (var i = 0; i < howManyTimes; i++)
        {
            message +=
                message
                + whatEvenIs?.ToString()
                + whatEvenIsThis?.ToString()
                + whatEvenIsThisT?.ToString()
                + evenMoreWhatIsThis?.GetType()
                + previousTricks?.GetType()
                + tuple?.GetType()
                + whatAmIDoing?.GetType();
        }
    }

    public Biscuit Rollover(Guid clientId, short timesToRun, DogTrick trick)
    {
        var biscuit = new Biscuit
        {
            Id = clientId,
            Message = trick.Message
        };

        Sit("Sit!", timesToRun);

        return biscuit;
    }

    public async Task<Biscuit<T1>> StayAndLayDown<TM1, TM2>(Guid clientId, short timesToRun, DogTrick<T1> trick, TM1 extraTreat, TM2 extraExtraTreat)
    {
        await Task.Delay(5);
        var biscuit = new Biscuit<T1>();
        biscuit.Treats.Add(extraTreat);
        biscuit.Treats.Add(extraExtraTreat);
        return await Task.FromResult(biscuit);
    }
}
