// <copyright file="MongoDbController.cs" company="OpenTelemetry Authors">
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

using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Examples.AspNetCoreMvc.Controllers;

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
