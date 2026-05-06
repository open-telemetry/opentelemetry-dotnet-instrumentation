/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_CALLTARGET_TRAMPOLINE_H_
#define OTEL_CLR_PROFILER_CALLTARGET_TRAMPOLINE_H_

#include "cor.h"
#include "corprof.h"
#include "string_utils.h"

namespace trace
{

HRESULT GenerateCallTargetTrampolineType(ICorProfilerInfo7* profilerInfo,
                                         ModuleID moduleId,
                                         const WSTRING& profilerAssemblyName);

} // namespace trace

#endif // OTEL_CLR_PROFILER_CALLTARGET_TRAMPOLINE_H_