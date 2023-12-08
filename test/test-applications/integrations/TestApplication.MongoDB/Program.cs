// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using TestApplication.Shared;

namespace TestApplication.MongoDB;

public static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var mongoPort = GetMongoPort(args);
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
        var database = client.GetDatabase("test-db");
        var collection = database.GetCollection<BsonDocument>("employees");

        Run(collection, newDocument);
        RunAsync(collection, newDocument).Wait();

#if !MONGODB_2_15_OR_GREATER
        WireProtocolExecuteIntegrationTest(client);
#endif
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
        var options = new FindOptions
        {
#if !MONGODB_2_15_OR_GREATER
#pragma warning disable 0618 // 'FindOptionsBase.Modifiers' is obsolete: 'Use individual properties instead.'
            Modifiers = new BsonDocument("$explain", true)
#pragma warning restore 0618
#endif
        };
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

        await collection.DeleteManyAsync(allFilter);
        await collection.InsertOneAsync(newDocument);

        var count = await collection.CountDocumentsAsync(new BsonDocument());

        Console.WriteLine($"Documents: {count}");

        var find = await collection.FindAsync(allFilter);
        var allDocuments = await find.ToListAsync();
        Console.WriteLine(allDocuments.FirstOrDefault());
    }

#if !MONGODB_2_15_OR_GREATER
    public static void WireProtocolExecuteIntegrationTest(MongoClient client)
    {
        var server = client.Cluster.SelectServer(new ServerSelector(), CancellationToken.None);
        var channel = server.GetChannel(CancellationToken.None);
        channel.KillCursors(new long[] { 0, 1, 2 }, new global::MongoDB.Driver.Core.WireProtocol.Messages.Encoders.MessageEncoderSettings(), CancellationToken.None);

        server = client.Cluster.SelectServer(new ServerSelector(), CancellationToken.None);
        channel = server.GetChannel(CancellationToken.None);
        channel.KillCursorsAsync(new long[] { 0, 1, 2 }, new global::MongoDB.Driver.Core.WireProtocol.Messages.Encoders.MessageEncoderSettings(), CancellationToken.None).Wait();
    }
#endif

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
}

#if !MONGODB_2_15_OR_GREATER
#pragma warning disable SA1402 // File may only contain a single type
internal class ServerSelector : global::MongoDB.Driver.Core.Clusters.ServerSelectors.IServerSelector
{
    public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
    {
        return servers;
    }
}
#pragma warning restore SA1402 // File may only contain a single type
#endif
