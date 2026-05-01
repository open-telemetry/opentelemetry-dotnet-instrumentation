// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "native_stack_walker.h"
#include "logger.h"

#if defined(_WIN32) && defined(_M_AMD64)
#include "rtl_stack_walk.h"
#include "thread_suspend.h"
#endif

namespace ProfilerStackCapture
{

#if defined(_WIN32) && defined(_M_AMD64)

/// @brief RTL-based native stack walker for Windows x64.
/// Delegates to WalkNativeStack / WalkNativeStackForThread in rtl_stack_walk.
class RtlNativeStackWalker : public INativeStackWalker
{
public:
    HRESULT WalkThread(IProfilerApi*                                       profilerApi,
                       ThreadID                                            managedThreadId,
                       ProfilerStackCapture::StackSnapshotCallbackContext* clientData) override
    {
        return WalkNativeStackForThread(profilerApi, managedThreadId, clientData);
    }
    HRESULT WalkSuspendedThread(void*                                               suspendedThread,
                                IProfilerApi*                                       profilerApi,
                                ProfilerStackCapture::StackSnapshotCallbackContext* clientData) override
    {
        NativeWalkContext ctx{clientData, nullptr, profilerApi};
        return WalkNativeStack(suspendedThread, &ctx);
    }
};

#endif // defined(_WIN32) && defined(_M_AMD64)

std::unique_ptr<INativeStackWalker> CreateNativeStackWalker()
{
#if defined(_WIN32) && defined(_M_AMD64)
    trace::Logger::Info("[NativeStackWalker] Created RtlNativeStackWalker (Windows x64)");
    return std::make_unique<RtlNativeStackWalker>();
#else
    trace::Logger::Debug("[NativeStackWalker] Native stack walking not available on this platform");
    return nullptr;
#endif
}

} // namespace ProfilerStackCapture