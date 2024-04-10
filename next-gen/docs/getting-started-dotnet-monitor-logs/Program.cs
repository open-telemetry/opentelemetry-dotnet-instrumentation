// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
