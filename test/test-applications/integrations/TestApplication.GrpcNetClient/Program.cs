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
#if NETFRAMEWORK
using System.Net.Http;
#endif
using System.Threading.Tasks;
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
