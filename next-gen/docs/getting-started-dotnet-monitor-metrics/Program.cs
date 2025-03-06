// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;

namespace OpenTelemetry;

internal static class Program
{
    private static readonly Meter s_MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
    private static readonly Counter<long> s_MyFruitCounter = s_MyMeter.CreateCounter<long>("MyFruitCounter");

    public static void Main()
    {
        Console.WriteLine("Press any key to exit");

        while (!Console.KeyAvailable)
        {
            s_MyFruitCounter.Add(1, new("name", "apple"), new("color", "red"));
            s_MyFruitCounter.Add(2, new("name", "lemon"), new("color", "yellow"));
            s_MyFruitCounter.Add(1, new("name", "lemon"), new("color", "yellow"));
            s_MyFruitCounter.Add(2, new("name", "apple"), new("color", "green"));
            s_MyFruitCounter.Add(5, new("name", "apple"), new("color", "red"));
            s_MyFruitCounter.Add(4, new("name", "lemon"), new("color", "yellow"));

            Thread.Sleep(300);
        }
    }
}
