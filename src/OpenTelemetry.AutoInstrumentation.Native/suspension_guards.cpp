// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "suspension_guards.h"

#include "runtime_capture.h"

namespace ProfilerStackCapture
{

RuntimeGuard::RuntimeGuard(IRuntimeCapture* runtime)
    : runtime_(runtime), active_(false)
{
    if (runtime_ != nullptr)
    {
        active_ = SUCCEEDED(runtime_->SuspendRuntime());
    }
}

RuntimeGuard::~RuntimeGuard()
{
    if (active_)
    {
        runtime_->ResumeRuntime();
    }
}

} // namespace ProfilerStackCapture