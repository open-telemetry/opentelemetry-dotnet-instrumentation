// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
await containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

var exists = await containerClient.ExistsAsync().ConfigureAwait(false);

Console.WriteLine(exists);

static string GetBlobServicePortPort(string[] args)
{
    if (args.Length == 1)
    {
        return args[0];
    }

    return "10000";
}
