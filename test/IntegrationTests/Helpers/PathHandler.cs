// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using Microsoft.AspNetCore.Http;

namespace IntegrationTests.Helpers;

#pragma warning disable CA1812 // Mark members as static. There is some issue in dotnet format.
internal sealed class PathHandler
#pragma warning restore CA1812 // Mark members as static. There is some issue in dotnet format.
{
    public PathHandler(RequestDelegate @delegate, string path)
    {
        Delegate = @delegate;
        Path = path;
    }

    public RequestDelegate Delegate { get; }

    public string Path { get; }
}
#endif
