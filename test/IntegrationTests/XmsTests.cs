// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

[Collection(XmsCollectionFixture.Name)]
public class XmsTests : TestHelper
{
    private const string PublishOperationAttributeValue = "publish";
    private const string ReceiveOperationAttributeValue = "receive";
    private const string DeliverOperationAttributeValue = "deliver";
    private const string MessagingSystemAttributeName = "messaging.system";
    private const string MessagingOperationAttributeName = "messaging.operation";
    private const string MessagingDestinationAttributeName = "messaging.destination.name";
    private const string MessagingMessageIdAttributeName = "messaging.message.id";
    private const string IbmMqMessagingSystemAttributeValue = "ibmmq";
    private const string XmsInstrumentationScopeName = "OpenTelemetry.AutoInstrumentation.Xms";

    // Pre-provisioned by the IBM MQ developer image's default configuration; not created by this test.
    private const string SyncQueueName = "DEV.QUEUE.1";
    private const string AsyncQueueName = "DEV.QUEUE.2";

    private readonly XmsFixture _xms;

    public XmsTests(ITestOutputHelper testOutputHelper, XmsFixture xms)
        : base("Xms", testOutputHelper)
    {
        _xms = xms;
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Xms), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        _xms.SkipIfUnsupportedPlatform();

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect(
            XmsInstrumentationScopeName,
            VersionHelper.AutoInstrumentationVersion,
            span => span.Kind == Span.Types.SpanKind.Producer && ValidateSpan(span, SyncQueueName, PublishOperationAttributeValue),
            "Publish to the synchronous-receive queue.");
        collector.Expect(
            XmsInstrumentationScopeName,
            VersionHelper.AutoInstrumentationVersion,
            span => span.Kind == Span.Types.SpanKind.Consumer && ValidateSpan(span, SyncQueueName, ReceiveOperationAttributeValue),
            "Synchronous receive from the queue.");
        collector.Expect(
            XmsInstrumentationScopeName,
            VersionHelper.AutoInstrumentationVersion,
            span => span.Kind == Span.Types.SpanKind.Producer && ValidateSpan(span, AsyncQueueName, PublishOperationAttributeValue),
            "Publish to the asynchronous-delivery queue.");
        collector.Expect(
            XmsInstrumentationScopeName,
            VersionHelper.AutoInstrumentationVersion,
            span => span.Kind == Span.Types.SpanKind.Consumer && ValidateSpan(span, AsyncQueueName, DeliverOperationAttributeValue),
            "Asynchronous delivery from the queue via a registered MessageListener.");

        collector.ExpectCollected(ValidatePropagation);

        EnableBytecodeInstrumentation();

        RunTestApplication(new TestSettings
        {
            PackageVersion = packageVersion,
            Arguments = $"--xms {_xms.Port} --sync-queue {SyncQueueName} --async-queue {AsyncQueueName}"
        });

        collector.AssertExpectations();
    }

    private static bool ValidateSpan(Span span, string destinationName, string operationName)
    {
        var messagingSystem = span.Attributes.SingleOrDefault(kv => kv.Key == MessagingSystemAttributeName)?.Value.StringValue;
        var messagingOperation = span.Attributes.SingleOrDefault(kv => kv.Key == MessagingOperationAttributeName)?.Value.StringValue;
        var destination = span.Attributes.SingleOrDefault(kv => kv.Key == MessagingDestinationAttributeName)?.Value.StringValue;
        var messageId = span.Attributes.SingleOrDefault(kv => kv.Key == MessagingMessageIdAttributeName)?.Value.StringValue;

        return messagingSystem == IbmMqMessagingSystemAttributeValue &&
               messagingOperation == operationName &&
               destination == destinationName &&
               !string.IsNullOrEmpty(messageId) &&
               span.Name == $"{destinationName} {operationName}";
    }

    private static bool ValidatePropagation(ICollection<MockSpansCollector.Collected> collectedSpans)
    {
        return TraceIdsMatch(collectedSpans, SyncQueueName, ReceiveOperationAttributeValue) &&
               TraceIdsMatch(collectedSpans, AsyncQueueName, DeliverOperationAttributeValue);
    }

    private static bool TraceIdsMatch(ICollection<MockSpansCollector.Collected> collectedSpans, string destinationName, string consumerOperationName)
    {
        var producerSpan = collectedSpans.SingleOrDefault(c => c.Span.Kind == Span.Types.SpanKind.Producer && c.Span.Name == $"{destinationName} {PublishOperationAttributeValue}");
        var consumerSpan = collectedSpans.SingleOrDefault(c => c.Span.Kind == Span.Types.SpanKind.Consumer && c.Span.Name == $"{destinationName} {consumerOperationName}");

        return producerSpan is not null && consumerSpan is not null && producerSpan.Span.TraceId == consumerSpan.Span.TraceId;
    }
}
