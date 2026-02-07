// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MongoDB.Bson;
using MongoDB.Driver;
using TestApplication.Shared;

namespace TestApplication.MongoDB;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var mongoPort = GetMongoPort(args);
        var mongoDatabase = GetMongoDbName(args);
        var mongoCollection = GetMongoCollectionName(args);
        var shouldTriggerError = ShouldTriggerError(args);
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

#if MONGODB_3_OR_GREATER
        using var client = new MongoClient(connectionString);
#else
        var client = new MongoClient(connectionString);
#endif
        var database = client.GetDatabase(mongoDatabase);
        var collection = database.GetCollection<BsonDocument>(mongoCollection);

        if (shouldTriggerError)
        {
            RunWithError(collection, newDocument);
        }
        else
        {
            Run(collection, newDocument);
            RunAsync(collection, newDocument).Wait();
        }
    }

    public static void RunWithError(IMongoCollection<BsonDocument> collection, BsonDocument newDocument)
    {
        try
        {
            // Trigger an error by trying to create an index with invalid options
            // This will cause a MongoCommandException
            var keys = Builders<BsonDocument>.IndexKeys.Ascending("invalid_field");
            var options = new CreateIndexOptions
            {
                Unique = true,
                Name = string.Empty // Empty name will cause an error
            };
            var indexModel = new CreateIndexModel<BsonDocument>(keys, options);
            collection.Indexes.CreateOne(indexModel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Expected error occurred: {ex.GetType().Name}");
            // Error should be captured in traces
        }
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

    private static bool ShouldTriggerError(string[] args)
    {
        return args.Any(arg => arg.Equals("--trigger-error", StringComparison.OrdinalIgnoreCase));
    }
}
