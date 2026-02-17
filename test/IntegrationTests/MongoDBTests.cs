// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(MongoDBCollectionFixture.Name)]
public class MongoDBTests : TestHelper
{
    private const string MongoDBInstrumentationScopeName = "OpenTelemetry.AutoInstrumentation.MongoDB";
    private const string MongoDbNamespace = "test-db";
    private const string MongoDbCollectionName = "employees";
    private const string MongoDbSystem = "mongodb";

    private const string DbSystemNameAttributeName = "db.system.name";
    private const string DbCollectionNameAttributeName = "db.collection.name";
    private const string DbNamespaceAttributeName = "db.namespace";
    private const string DbOperationNameAttributeName = "db.operation.name";
    private const string DbOperationBatchSizeAttributeName = "db.operation.batch.size";

    private const string ServerAddressAttributeName = "server.address";
    private const string ServerPortAttributeName = "server.port";
    private const string ErrorTypeAttributeName = "error.type";
    private const string ExceptionEventName = "exception";
    private const string DbResponseStatusCodeAttributeName = "db.response.status_code";

    private readonly MongoDBFixture _mongoDB;

    public MongoDBTests(ITestOutputHelper output, MongoDBFixture mongoDB)
        : base("MongoDB", output)
    {
        _mongoDB = mongoDB;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Any")]
    [MemberData(nameof(LibraryVersion.MongoDB), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        const int spanCount = 3;
        for (var i = 0; i < spanCount; i++)
        {
            collector.Expect(MongoDBInstrumentationScopeName, VersionHelper.AutoInstrumentationVersion);
        }

        collector.Expect(MongoDBInstrumentationScopeName, VersionHelper.AutoInstrumentationVersion, ValidateSpan, schemaUrl: "https://opentelemetry.io/schemas/1.39.0");

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
#if NET462
            Framework = string.IsNullOrEmpty(packageVersion) || new Version(packageVersion) >= new Version(3, 0, 0) ? "net472" : "net462",
#endif
            Arguments = $"--mongo-db {_mongoDB.Port} {MongoDbNamespace} {MongoDbCollectionName}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Any")]
    [MemberData(nameof(LibraryVersion.MongoDB), MemberType = typeof(LibraryVersion))]
    public void SubmitsTracesWithErrorDetails(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        // Expect at least one span with error details
        collector.Expect(MongoDBInstrumentationScopeName, VersionHelper.AutoInstrumentationVersion, ValidateErrorSpan, schemaUrl: "https://opentelemetry.io/schemas/1.39.0");

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
#if NET462
            Framework = string.IsNullOrEmpty(packageVersion) || new Version(packageVersion) >= new Version(3, 0, 0) ? "net472" : "net462",
#endif
            Arguments = $"--mongo-db {_mongoDB.Port} {MongoDbNamespace} {MongoDbCollectionName} --trigger-error",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    private static bool ValidateDatabaseAttributes(IReadOnlyCollection<KeyValue> spanAttributes)
    {
        var collectionNameAttr = spanAttributes.FirstOrDefault(kv => kv.Key == DbCollectionNameAttributeName);
        var dbNamespaceAttr = spanAttributes.FirstOrDefault(kv => kv.Key == DbNamespaceAttributeName);
        var dbSystemAttr = spanAttributes.FirstOrDefault(kv => kv.Key == DbSystemNameAttributeName);
        var dbOperationNameAttr = spanAttributes.FirstOrDefault(kv => kv.Key == DbOperationNameAttributeName);

        if (collectionNameAttr == null ||
            dbNamespaceAttr == null ||
            dbSystemAttr == null ||
            dbOperationNameAttr == null)
        {
            return false;
        }

        var collectionName = collectionNameAttr.Value.StringValue;
        var dbNamespace = dbNamespaceAttr.Value.StringValue;
        var dbSystem = dbSystemAttr.Value.StringValue;
        var dbOperationName = dbOperationNameAttr.Value.StringValue;

        if (collectionName != MongoDbCollectionName ||
            dbNamespace != MongoDbNamespace ||
            dbSystem != MongoDbSystem ||
            string.IsNullOrWhiteSpace(dbOperationName))
        {
            return false;
        }

        if (dbOperationName is "insert" or "update" or "delete")
        {
            var batchSizeAttr = spanAttributes.FirstOrDefault(kv => kv.Key == DbOperationBatchSizeAttributeName);
            if (batchSizeAttr == null || batchSizeAttr.Value.IntValue <= 0)
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidateSpan(Span span)
    {
        return span.Kind == Span.Types.SpanKind.Client && ValidateDatabaseAttributes(span.Attributes) && ValidateNetworkAttributes(span.Attributes);
    }

    private bool ValidateErrorSpan(Span span)
    {
        var hasErrorType = span.Attributes.Any(kv => kv.Key == ErrorTypeAttributeName && !string.IsNullOrEmpty(kv.Value.StringValue));
        var hasExceptionDetails = span.Events.Any(e => e.Name == ExceptionEventName);
        var hasErrorStatus = span.Status?.Code == Status.Types.StatusCode.Error;
        var hasDbResponseStatusCode = span.Attributes.Any(kv => kv.Key == DbResponseStatusCodeAttributeName && kv.Value.IntValue > 0);

        return span.Kind == Span.Types.SpanKind.Client &&
               ValidateDatabaseAttributes(span.Attributes) &&
               ValidateNetworkAttributes(span.Attributes) &&
               (hasErrorType || hasExceptionDetails || hasErrorStatus) &&
               hasDbResponseStatusCode;
    }

    private bool ValidateNetworkAttributes(IReadOnlyCollection<KeyValue> spanAttributes)
    {
        var serverAddressAttr = spanAttributes.FirstOrDefault(kv => kv.Key == ServerAddressAttributeName);
        var serverPortAttr = spanAttributes.FirstOrDefault(kv => kv.Key == ServerPortAttributeName);

        if (serverAddressAttr == null || serverPortAttr == null)
        {
            return false;
        }

        var serverAddress = serverAddressAttr.Value.StringValue;
        var serverPort = serverPortAttr.Value.IntValue;

        return (serverAddress == "localhost" || serverAddress is "127.0.0.1" or "::1") &&
               serverPort == _mongoDB.Port;
    }
}
