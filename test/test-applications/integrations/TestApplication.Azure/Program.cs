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

using Azure.Storage.Blobs;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var port = GetBlobServicePortPort(args);

// connection string based on https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio#well-known-storage-account-and-key
var developerConnectionString = $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:{port}/devstoreaccount1;";

// Create a blob service client using the developer connection string
var blobServiceClient = new BlobServiceClient(developerConnectionString);

// Generate a random container name
var containerName = $"test-container-{Guid.NewGuid()}";

// Create a blob container client
var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

// Create the container if it does not exist
await containerClient.CreateIfNotExistsAsync();

var exists = await containerClient.ExistsAsync();

Console.WriteLine(exists);

static string GetBlobServicePortPort(string[] args)
{
    if (args.Length == 1)
    {
        return args[0];
    }

    return "10000";
}
