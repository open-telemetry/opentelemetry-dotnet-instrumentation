// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal enum SampleType
{
    Continuous = 1,
    SelectedThreads = 2,
    Allocation = 3
}
#endif
