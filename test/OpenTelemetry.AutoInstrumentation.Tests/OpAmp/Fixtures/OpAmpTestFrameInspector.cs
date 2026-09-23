// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using Google.Protobuf;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

internal static class OpAmpTestFrameInspector
{
    private const int CustomCapabilitiesFieldNumber = 12;

    public static bool ContainsCustomCapabilities(HttpRequestMessage request)
    {
        var content = request.Content!.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        using var input = new CodedInputStream(content);
        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            if (WireFormat.GetTagFieldNumber(tag) == CustomCapabilitiesFieldNumber)
            {
                return true;
            }

            input.SkipLastField();
        }

        return false;
    }
}
