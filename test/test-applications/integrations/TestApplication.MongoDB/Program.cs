// <copyright file="Program.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics;
using System.Linq;
#if !MONGODB_2_15
using System.Threading;
#endif
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using TestApplication.Shared;

namespace TestApplication.MongoDB;

public static class Program
{
    internal static readonly ActivitySource ActivitySource = new("TestApplication.MongoDB", "1.0.0");

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

        using var mainScope = ActivitySource.StartActivity("Main()");
        var connectionString = $"mongodb://{Host()}:{mongoPort}";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("test-db");
        var collection = database.GetCollection<BsonDocument>("employees");

        Run(collection, newDocument);
        RunAsync(collection, newDocument).Wait();

#if !MONGODB_2_15
        WireProtocolExecuteIntegrationTest(client);
#endif
    }

    public static void Run(IMongoCollection<BsonDocument> collection, BsonDocument newDocument)
    {
        var allFilter = new BsonDocument();

        using var syncScope = ActivitySource.StartActivity("sync-calls");
        collection.DeleteMany(allFilter);
        collection.InsertOne(newDocument);

#if MONGODB_2_7
        var count = collection.CountDocuments(new BsonDocument());
#else
        var count = collection.Count(new BsonDocument());
#endif
        Console.WriteLine($"Documents: {count}");

        var find = collection.Find(allFilter);
        var allDocuments = find.ToList();
        Console.WriteLine(allDocuments.FirstOrDefault());

        // Run an explain query to invoke problematic MongoDB.Driver.Core.Operations.FindOpCodeOperation<TDocument>
        // https://stackoverflow.com/questions/49506857/how-do-i-run-an-explain-query-with-the-2-4-c-sharp-mongo-driver
        var options = new FindOptions
        {
#if !MONGODB_2_15
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

        using var asyncScope = ActivitySource.StartActivity("async-calls");
        await collection.DeleteManyAsync(allFilter);
        await collection.InsertOneAsync(newDocument);

#if MONGODB_2_7
        var count = await collection.CountDocumentsAsync(new BsonDocument());
#else
        var count = await collection.CountAsync(new BsonDocument());
#endif

        Console.WriteLine($"Documents: {count}");

        var find = await collection.FindAsync(allFilter);
        var allDocuments = await find.ToListAsync();
        Console.WriteLine(allDocuments.FirstOrDefault());
    }

#if !MONGODB_2_15
    public static void WireProtocolExecuteIntegrationTest(MongoClient client)
    {
        using (var syncScope = ActivitySource.StartActivity("sync-calls-execute"))
        {
            var server = client.Cluster.SelectServer(new ServerSelector(), CancellationToken.None);
            var channel = server.GetChannel(CancellationToken.None);
            channel.KillCursors(new long[] { 0, 1, 2 }, new global::MongoDB.Driver.Core.WireProtocol.Messages.Encoders.MessageEncoderSettings(), CancellationToken.None);
        }

        using (var asyncScope = ActivitySource.StartActivity("async-calls-execute"))
        {
            var server = client.Cluster.SelectServer(new ServerSelector(), CancellationToken.None);
            var channel = server.GetChannel(CancellationToken.None);
            channel.KillCursorsAsync(new long[] { 0, 1, 2 }, new global::MongoDB.Driver.Core.WireProtocol.Messages.Encoders.MessageEncoderSettings(), CancellationToken.None).Wait();
        }
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

#if !MONGODB_2_15
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
