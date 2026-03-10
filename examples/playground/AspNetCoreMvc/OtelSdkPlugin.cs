// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace Examples.AspNetCoreMvc;

public class OtelSdkPlugin
{
    public void Initializing()
    {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
        Console.WriteLine("Hello from OtelSdkPlugin");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
    }
}
