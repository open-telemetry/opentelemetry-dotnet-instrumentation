// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace TestApplication.AWS;

public class DDBOperation
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DDBOperation(IAmazonDynamoDB ddb, string tableName)
    {
        _dynamoDb = ddb;
        _tableName = tableName;
    }

    public async Task CreateTable()
    {
        var request = new ListTablesRequest
        {
            Limit = 10
        };

        var response = await _dynamoDb.ListTablesAsync(request);

        var results = response.TableNames;

        if (!results.Contains(_tableName))
        {
            var createRequest = new CreateTableRequest
            {
                TableName = _tableName,
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = "Id",
                        AttributeType = "S"
                    }
                },
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "Id",
                        KeyType = "HASH"
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 2,
                    WriteCapacityUnits = 2
                }
            };

            await _dynamoDb.CreateTableAsync(createRequest);
        }
    }

    public async Task<string> InsertRow()
    {
        string guid = Guid.NewGuid().ToString();
        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "Id", new AttributeValue { S = guid } },
                { "Title", new AttributeValue { S = DateTime.Now.ToShortDateString() } }
            }
        };

        await _dynamoDb.PutItemAsync(request);
        return guid;
    }

    public async Task<bool> SelectRow(string id)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue> { { "Id", new AttributeValue { S = id.ToString() } } }
        };

        var response = await _dynamoDb.GetItemAsync(request);

        return (response != null && response.IsItemSet);
    }
}
