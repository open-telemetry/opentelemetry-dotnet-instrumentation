// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

public class Plugin
{
    public Tuple<bool, uint, bool, uint> GetContinuousProfilerConfiguration()
    {
        return Tuple.Create(true, 123u, true, 3u);
    }
}
