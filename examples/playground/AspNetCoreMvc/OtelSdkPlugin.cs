// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace Examples.AspNetCoreMvc;

public class OtelSdkPlugin
{
    public void Initializing()
    {
        Console.WriteLine("Hello from OtelSdkPlugin");
    }
}
