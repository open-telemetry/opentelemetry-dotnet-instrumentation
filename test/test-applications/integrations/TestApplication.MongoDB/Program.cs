// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MongoDB.Bson;
using MongoDB.Driver;
using TestApplication.Shared;

namespace TestApplication.MongoDB;

public static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var mongoPort = GetMongoPort(args);
        var mongoDatabase = GetMongoDbName(args);
        var mongoCollection = GetMongoCollectionName(args);
        var newDocument = new BsonDocument
        {
            { "name", "MongoDB" },
            { "type", "Database" },
            { "count", 1 },
            {
                "info", new BsonDocument
                {
                    { "x", 203 },
                    { "y", 102 }
                }
            }
        };

        var connectionString = $"mongodb://{Host()}:{mongoPort}";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(mongoDatabase);
        var collection = database.GetCollection<BsonDocument>(mongoCollection);

        Run(collection, newDocument);
        RunAsync(collection, newDocument).Wait();
    }

    public static void Run(IMongoCollection<BsonDocument> collection, BsonDocument newDocument)
    {
        var allFilter = new BsonDocument();

        collection.DeleteMany(allFilter);
        collection.InsertOne(newDocument);

        var count = collection.CountDocuments(new BsonDocument());

        Console.WriteLine($"Documents: {count}");

        var find = collection.Find(allFilter);
        var allDocuments = find.ToList();
        Console.WriteLine(allDocuments.FirstOrDefault());

        // Run an explain query to invoke problematic MongoDB.Driver.Core.Operations.FindOpCodeOperation<TDocument>
        // https://stackoverflow.com/questions/49506857/how-do-i-run-an-explain-query-with-the-2-4-c-sharp-mongo-driver
        var options = new FindOptions();
        // Without properly unboxing generic arguments whose instantiations
        // are valuetypes, the following line will fail with
        // System.EntryPointNotFoundException: Entry point was not found.
        var cursor = collection.Find(x => true, options).ToCursor();
        foreach (var document in cursor.ToEnumerable())
        {
            Console.WriteLine(document);
        }
    }

    public static async Task RunAsync(IMongoCollection<BsonDocument> collection, BsonDocument newDocument)
    {
        var allFilter = new BsonDocument();

        await collection.DeleteManyAsync(allFilter).ConfigureAwait(false);
        await collection.InsertOneAsync(newDocument).ConfigureAwait(false);

        var count = await collection.CountDocumentsAsync(new BsonDocument()).ConfigureAwait(false);

        Console.WriteLine($"Documents: {count}");

        var find = await collection.FindAsync(allFilter).ConfigureAwait(false);
        var allDocuments = await find.ToListAsync().ConfigureAwait(false);
        Console.WriteLine(allDocuments.FirstOrDefault());
    }

    private static string Host()
    {
        return Environment.GetEnvironmentVariable("MONGO_HOST") ?? "localhost";
    }

    private static string GetMongoPort(string[] args)
    {
        if (args.Length > 1)
        {
            return args[1];
        }

        return "27017";
    }

    private static string GetMongoDbName(string[] args)
    {
        if (args.Length > 2)
        {
            return args[2];
        }

        return "test-db";
    }

    private static string GetMongoCollectionName(string[] args)
    {
        if (args.Length > 3)
        {
            return args[3];
        }

        return "employees";
    }
}
