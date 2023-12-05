/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include <corprof.h>

#ifndef OTEL_CONTINUOUS_PROFILER_H_
#define OTEL_CONTINUOUS_PROFILER_H_

namespace continuous_profiler
{
class ContinuousProfiler
{
public:
    void StartThreadSampling();
    void StartAllocationSampling();
    ICorProfilerInfo12* info12;
    void SetGlobalInfo12(ICorProfilerInfo12* cor_profiler_info12);
};
} // namespace continuous_profiler

#endif // OTEL_CONTINUOUS_PROFILER_H_
