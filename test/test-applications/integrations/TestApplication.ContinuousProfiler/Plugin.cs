// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

public class Plugin
{
    public Tuple<bool, bool, uint> GetContinuousProfilerConfiguration()
    {
        return Tuple.Create(true, true, 123u);
    }
}
