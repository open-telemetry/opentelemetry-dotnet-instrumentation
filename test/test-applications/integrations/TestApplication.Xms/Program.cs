// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using IBM.XMS;
using TestApplication.Shared;

namespace TestApplication.Xms;

internal static class Program
{
    private const string QueueManagerName = "QM1";
    private const string ChannelName = "DEV.APP.SVRCONN";
    private const string UserId = "app";
    private const string Password = "passw0rd";
    private const string SyncMessageText = "sync-message";
    private const string AsyncMessageText = "async-message";

    private static readonly ManualResetEventSlim AsyncMessageReceived = new(false);
    private static IMessage? _asyncMessage;

    public static int Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var port = int.Parse(ArgumentHelper.GetRequiredArgument(args, "--xms"), CultureInfo.InvariantCulture);
        var syncQueueName = ArgumentHelper.GetRequiredArgument(args, "--sync-queue");
        var asyncQueueName = ArgumentHelper.GetRequiredArgument(args, "--async-queue");

        var connectionFactory = XMSFactoryFactory.GetInstance(XMSC.CT_WMQ).CreateConnectionFactory();
        connectionFactory.SetStringProperty(XMSC.WMQ_HOST_NAME, "localhost");
        connectionFactory.SetIntProperty(XMSC.WMQ_PORT, port);
        connectionFactory.SetStringProperty(XMSC.WMQ_CHANNEL, ChannelName);
        connectionFactory.SetStringProperty(XMSC.WMQ_QUEUE_MANAGER, QueueManagerName);
        connectionFactory.SetIntProperty(XMSC.WMQ_CONNECTION_MODE, XMSC.WMQ_CM_CLIENT);

        using var connection = CreateConnectionWithRetry(connectionFactory);
        using var session = connection.CreateSession(false, AcknowledgeMode.AutoAcknowledge);

        var syncQueue = session.CreateQueue(syncQueueName);
        var asyncQueue = session.CreateQueue(asyncQueueName);

        using var syncProducer = session.CreateProducer(syncQueue);
        using var syncConsumer = session.CreateConsumer(syncQueue);

        using var asyncProducer = session.CreateProducer(asyncQueue);
        using var asyncConsumer = session.CreateConsumer(asyncQueue);
        asyncConsumer.MessageListener = OnAsyncMessage;

        connection.Start();

        syncProducer.Send(session.CreateTextMessage(SyncMessageText));

        var received = syncConsumer.Receive(10_000);
        if (received is not ITextMessage receivedText || receivedText.Text != SyncMessageText)
        {
            Console.WriteLine("Synchronous receive did not return the expected message.");
            return 1;
        }

        Console.WriteLine("Synchronous receive completed.");

        asyncProducer.Send(session.CreateTextMessage(AsyncMessageText));

        if (!AsyncMessageReceived.Wait(TimeSpan.FromSeconds(15)))
        {
            Console.WriteLine("Timed-out waiting for asynchronous message delivery.");
            return 1;
        }

        if (_asyncMessage is not ITextMessage asyncText || asyncText.Text != AsyncMessageText)
        {
            Console.WriteLine("Asynchronous delivery did not return the expected message.");
            return 1;
        }

        Console.WriteLine("Asynchronous delivery completed.");

        return 0;
    }

    private static void OnAsyncMessage(IMessage message)
    {
        _asyncMessage = message;
        AsyncMessageReceived.Set();
    }

    /// <summary>
    /// The container's TCP listener port opens before the queue manager has finished starting,
    /// so the first few connection attempts can fail even though the port is reachable.
    /// </summary>
    private static IConnection CreateConnectionWithRetry(IConnectionFactory connectionFactory)
    {
        const int maxAttempts = 30;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return connectionFactory.CreateConnection(UserId, Password);
            }
            catch (XMSException ex) when (attempt < maxAttempts)
            {
                Console.WriteLine($"CreateConnection attempt {attempt}/{maxAttempts} failed: {ex.Message}. Retrying...");
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        return connectionFactory.CreateConnection(UserId, Password);
    }
}
