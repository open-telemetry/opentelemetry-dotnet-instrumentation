// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ExampleLibrary.FakeClient;

public class DogClient<T1, T2>
{
    public void Silence()
    {
        Task.Delay(1).Wait();
    }

    public string TellMeIfTheCookieIsYummy(Biscuit.Cookie cookie, Biscuit.Cookie.Raisin raisin)
    {
#if NET
        ArgumentNullException.ThrowIfNull(cookie);
        ArgumentNullException.ThrowIfNull(raisin);
#else
        if (cookie == null)
        {
            throw new ArgumentNullException(nameof(cookie));
        }

        if (raisin == null)
        {
            throw new ArgumentNullException(nameof(raisin));
        }
#endif

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
        byte[]? whatEvenIs = null,
        Guid[][]? whatEvenIsThis = null,
        T1[][][]? whatEvenIsThisT = null,
#pragma warning disable CA1002 // Do not expose generic lists
        List<byte[][]>? evenMoreWhatIsThis = null,
        List<DogTrick<T1>>? previousTricks = null,
#pragma warning restore CA1002 // Do not expose generic lists
        Tuple<int, T1, string, object, Tuple<Tuple<T2, long>, long>, Task, Guid>? tuple = null,
        Dictionary<int, IList<Task<DogTrick<T1>>>>? whatAmIDoing = null)
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
#if NET
        ArgumentNullException.ThrowIfNull(trick);
#else
        if (trick == null)
        {
            throw new ArgumentNullException(nameof(trick));
        }
#endif

        var biscuit = new Biscuit
        {
            Id = clientId,
            Message = trick.Message
        };

        Sit("Sit!", timesToRun);

        return biscuit;
    }

    public async Task<Biscuit<T1>> StayAndLayDown<TMethod1, TMethod2>(Guid clientId, short timesToRun, DogTrick<T1> trick, TMethod1 extraTreat, TMethod2 extraExtraTreat)
    {
        await Task.Delay(5).ConfigureAwait(false);
        var biscuit = new Biscuit<T1>();
        biscuit.Treats.Add(extraTreat);
        biscuit.Treats.Add(extraExtraTreat);
        return biscuit;
    }
}
