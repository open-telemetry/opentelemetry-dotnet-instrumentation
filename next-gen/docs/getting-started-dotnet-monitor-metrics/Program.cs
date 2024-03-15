// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Program
{
    private static readonly Meter MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
    private static readonly Counter<long> MyFruitCounter = MyMeter.CreateCounter<long>("MyFruitCounter");

    public static void Main(string[] args)
    {
        Console.WriteLine("Press any key to exit");

        using IHost host = Host.CreateApplicationBuilder(args).Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        while (!Console.KeyAvailable)
        {

            logger.FoodPriceChanged("artichoke", 9.99);

            using (logger.BeginScope(new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("store", "Seattle"),
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

            MyFruitCounter.Add(1, new("name", "apple"), new("color", "red"));
            MyFruitCounter.Add(2, new("name", "lemon"), new("color", "yellow"));
            MyFruitCounter.Add(1, new("name", "lemon"), new("color", "yellow"));
            MyFruitCounter.Add(2, new("name", "apple"), new("color", "green"));
            MyFruitCounter.Add(5, new("name", "apple"), new("color", "red"));
            MyFruitCounter.Add(4, new("name", "lemon"), new("color", "yellow"));

            Thread.Sleep(1000);
        }
    }
}

internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Food `{name}` price changed to `{price}`.")]
    public static partial void FoodPriceChanged(this ILogger logger, string name, double price);

    [LoggerMessage(LogLevel.Critical, "A `{productType}` recall notice was published for `{brandName} {productDescription}` produced by `{companyName}` ({recallReasonDescription}).")]
    public static partial void FoodRecallNotice(
        this ILogger logger,
        string brandName,
        string productDescription,
        string productType,
        string recallReasonDescription,
        string companyName);
}
