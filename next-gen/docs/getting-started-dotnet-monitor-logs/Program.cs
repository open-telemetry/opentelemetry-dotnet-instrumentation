// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenTelemetry;

internal sealed class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        using var host = Host.CreateApplicationBuilder(args).Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        while (!Console.KeyAvailable)
        {
            logger.FoodPriceChanged("artichoke", 9.99);

            using (logger.BeginScope(new List<KeyValuePair<string, object>>
            {
                new("store", "Seattle"),
            }))
            {
                logger.FoodPriceChanged("truffle", 999.99);
            }

            logger.FoodRecallNotice(
                brandName: "Contoso",
                productDescription: "Salads",
                productType: "Food & Beverages",
                recallReasonDescription: "due to a possible health risk from Listeria monocytogenes",
                companyName: "Contoso Fresh Vegetables, Inc.");

            Thread.Sleep(300);
        }
    }
}
