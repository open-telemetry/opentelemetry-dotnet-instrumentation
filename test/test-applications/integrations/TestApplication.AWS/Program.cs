// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Amazon.DynamoDBv2;
using Amazon.Runtime;
using TestApplication.Shared;

namespace TestApplication.AWS;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
        string tableName = "SampleData";
        var clientConfig = new AmazonDynamoDBConfig { ServiceURL = "http://localhost:" + GetDDBServicePort(args) };
        var amazonDynamoDb = new AmazonDynamoDBClient(new BasicAWSCredentials("testId", "testKey"), clientConfig);
        var ddbOperation = new DDBOperation(amazonDynamoDb, tableName);
        await ddbOperation.CreateTable();
        string id = await ddbOperation.InsertRow();
        await ddbOperation.SelectRow(id);
    }

    private static string GetDDBServicePort(string[] args)
    {
        if (args.Length == 1)
        {
            return args[0];
        }

        return "8000";
    }
}
