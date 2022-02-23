using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Samples.AspNetCoreMvc.Controllers;

[Route("api/mongo")]
public class MongoDbController : ControllerBase
{
    private const string ConnectionString = "mongodb://localhost:27017";

    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        StringBuilder logStack = new StringBuilder();

        logStack.AppendLine($"Connecting to MongoDB: {ConnectionString}");
        MongoClient dbClient = new MongoClient(ConnectionString);
        logStack.AppendLine("Connected to MongoDB");

        logStack.AppendLine("Get Databases");
        var dbList = dbClient.ListDatabases().ToList();
        var dbNames = string.Join(", ", dbList.Select(db => db.GetValue("name")));
        logStack.AppendLine($"Result Databases: [{dbNames}]");

        logStack.AppendLine("Query startup_logs");
        var document = dbClient
            .GetDatabase("local")
            .GetCollection<BsonDocument>("startup_log")
            .Find(new BsonDocument())
            .FirstOrDefault();
        logStack.AppendLine($"Result startup_logs: {document}");

        return Ok(logStack.ToString());
    }
}