// <copyright file="WcfDotNetTests.cs" company="OpenTelemetry Authors">
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

// This test won't work outside of windows as it need the server side which is .NET Framework only.
#if NET6_0_OR_GREATER && _WINDOWS
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfDotNetTests : WcfTestsBase
{
    public WcfDotNetTests(ITestOutputHelper output)
        : base("Wcf.Client.DotNet", output)
    {
    }

    [Trait("Category", "EndToEnd")]
    [Theory]
    [MemberData(nameof(LibraryVersion.WCFCoreClient), MemberType = typeof(LibraryVersion))]
    public async Task SubmitTraces(string clientPackageVersion)
    {
        EnableBytecodeInstrumentation();
        await SubmitsTracesInternal(clientPackageVersion);
    }
}

#endif
