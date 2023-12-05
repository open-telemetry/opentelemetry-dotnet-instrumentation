// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "continuous_profiler.h"

namespace continuous_profiler
{
void ContinuousProfiler::SetGlobalInfo12(ICorProfilerInfo12* cor_profiler_info12)
{
    this->info12         = cor_profiler_info12;
    // this->helper.info12_ = cor_profiler_info12;
}
} // namespace continuous_profiler
