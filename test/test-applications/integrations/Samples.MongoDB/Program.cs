using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;

namespace Samples.MongoDB
{
    public static class Program
    {
        internal static readonly ActivitySource ActivitySource = new ActivitySource(
            "Samples.MongoDB", "1.0.0");

        private static string Host()
        {
            return Environment.GetEnvironmentVariable("MONGO_HOST") ?? "localhost";
        }

        public static void Main(string[] args)
        {
            Console.WriteLine($"Command line: {string.Join(";", args)}");
            Console.WriteLine($"Profiler attached: {IsProfilerAttached()}");
            Console.WriteLine($"Platform: {(Environment.Is64BitProcess ? "x64" : "x32")}");

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

            using (var mainScope = ActivitySource.StartActivity("Main()"))
            {
                var connectionString = $"mongodb://{Host()}:{mongoPort}";
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase("test-db");
                var collection = database.GetCollection<BsonDocument>("employees");

                Run(collection, newDocument);
                RunAsync(collection, newDocument).Wait();

#if MONGODB_2_2
                WireProtocolExecuteIntegrationTest(client);
#endif
            }
        }

        private static string GetMongoPort(string[] args)
        {
            if (args.Length > 0)
            {
                return args[1];
            }

            return "27017";
        }

        public static void Run(IMongoCollection<BsonDocument> collection, BsonDocument newDocument)
        {
            var allFilter = new BsonDocument();

            using (var syncScope = ActivitySource.StartActivity("sync-calls"))
            {
#if MONGODB_2_2
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
#pragma warning disable 0618 // 'FindOptionsBase.Modifiers' is obsolete: 'Use individual properties instead.'
                    Modifiers = new BsonDocument("$explain", true)
#pragma warning restore 0618
                };
                // Without properly unboxing generic arguments whose instantiations
                // are valuetypes, the following line will fail with
                // System.EntryPointNotFoundException: Entry point was not found.
                var cursor = collection.Find(x => true, options).ToCursor();
                foreach (var document in cursor.ToEnumerable())
                {
                    Console.WriteLine(document);
                }
#endif
            }
        }

        public static async Task RunAsync(IMongoCollection<BsonDocument> collection, BsonDocument newDocument)
        {
            var allFilter = new BsonDocument();

            using (var asyncScope = ActivitySource.StartActivity("async-calls"))
            {
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
        }
#if MONGODB_2_2

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

        internal class ServerSelector : global::MongoDB.Driver.Core.Clusters.ServerSelectors.IServerSelector
        {
            public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
            {
                return servers;
            }
        }
#endif

        private static bool? IsProfilerAttached()
        {
            var instrumentationType = Type.GetType("OpenTelemetry.ClrProfiler.Managed.Instrumentation, OpenTelemetry.ClrProfiler.Managed", throwOnError: false);

            if (instrumentationType == null)
            {
                return null;
            }

            var property = instrumentationType.GetProperty("ProfilerAttached");

            var isAttached = property?.GetValue(null) as bool?;

            return isAttached ?? false;
        }
    }
}
