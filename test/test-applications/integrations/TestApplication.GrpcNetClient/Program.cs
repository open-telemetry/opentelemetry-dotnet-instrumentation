// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Net.Http;
#endif
using Greet;
using Grpc.Core;
using Grpc.Net.Client;
#if NETFRAMEWORK
using Grpc.Net.Client.Web;
#endif
using TestApplication.Shared;

namespace TestApplication.GrpcNetClient;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        const string uri = "http://dummyAdress";
#if NETFRAMEWORK
        var channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(new HttpClientHandler())
        });
#else
        var channel = GrpcChannel.ForAddress(uri);
#endif

        try
        {
            var greeterClient = new Greeter.GreeterClient(channel);
            await greeterClient.SayHelloAsync(new HelloRequest());
        }
        catch (RpcException e)
        {
            Console.WriteLine(e);
        }
    }
}
