using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Samples.MongoDb
{
    public class Program
    {
        private const string ConnectionString = "mongodb://localhost:27017";

        public static int Main(string[] args)
        {
            Console.WriteLine("Connecting to MongoDB: {0}", ConnectionString);
            MongoClient dbClient = new MongoClient(ConnectionString);
            Console.WriteLine("Connected to MongoDB");

            Console.WriteLine("Get Databases");
            var dbList = dbClient.ListDatabases().ToList();
            Console.WriteLine("Result Databases: [{0}]", string.Join(", ", dbList.Select(db => db.GetValue("name"))));

            Console.WriteLine("Query startup_logs");
            var document = dbClient
                .GetDatabase("local")
                .GetCollection<BsonDocument>("startup_log")
                .Find(new BsonDocument())
                .FirstOrDefault();
            Console.WriteLine("Result startup_logs: {0}", document);

            return 0;
        }
    }
}
