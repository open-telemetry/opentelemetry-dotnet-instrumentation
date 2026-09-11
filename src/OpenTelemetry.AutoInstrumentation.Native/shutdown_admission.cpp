// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "shutdown_admission.h"

#include <atomic>
#include <condition_variable>
#include <cstdint>
#include <mutex>

namespace continuous_profiler
{
namespace
{
// The sampler's callback and publication boundaries are process-wide. The closing bit is the terminal transition;
// the remaining bits count operations that crossed admission before that transition.
constexpr uint64_t    kShutdownAdmissionClosingBit = uint64_t{1} << 63;
constexpr uint64_t    kShutdownAdmissionCountMask  = ~kShutdownAdmissionClosingBit;
std::atomic<uint64_t> shutdown_admission_gate{0};

// Cold-path wait primitives. Sampler callbacks and publications never take this mutex; they only perform the CAS
// admission and notify the condition variable when the final admitted operation leaves.
std::mutex              shutdown_mutex;
std::condition_variable shutdown_cv;

bool TryEnterShutdownAdmission() noexcept
{
    auto state = shutdown_admission_gate.load(std::memory_order_acquire);
    for (;;)
    {
        if ((state & kShutdownAdmissionClosingBit) != 0 ||
            (state & kShutdownAdmissionCountMask) == kShutdownAdmissionCountMask)
        {
            return false;
        }

        if (shutdown_admission_gate.compare_exchange_weak(state, state + 1, std::memory_order_acquire,
                                                          std::memory_order_relaxed))
        {
            return true;
        }
    }
}

void LeaveShutdownAdmission() noexcept
{
    const auto previous = shutdown_admission_gate.fetch_sub(1, std::memory_order_release);
    if ((previous & kShutdownAdmissionCountMask) == 1)
    {
        shutdown_cv.notify_all();
    }
}
} // namespace

ShutdownAdmission::ShutdownAdmission() noexcept : admitted_(TryEnterShutdownAdmission()) {}

ShutdownAdmission::~ShutdownAdmission() noexcept
{
    if (admitted_)
    {
        LeaveShutdownAdmission();
    }
}

void CloseShutdownAdmissions() noexcept
{
    shutdown_admission_gate.fetch_or(kShutdownAdmissionClosingBit, std::memory_order_acq_rel);
}

void WaitForShutdownAdmissions() noexcept
{
    std::unique_lock<std::mutex> lock(shutdown_mutex);
    shutdown_cv.wait(lock,
                     [] {
                         return (shutdown_admission_gate.load(std::memory_order_acquire) &
                                 kShutdownAdmissionCountMask) == 0;
                     });
}
} // namespace continuous_profiler
