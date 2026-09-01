// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/stack_walk_guard.h"

#include <chrono>

namespace
{

using ProfilerStackCapture::IProfilerApi;
using ProfilerStackCapture::StackWalkGuard;

class FakeProfilerApi final : public IProfilerApi
{
public:
    HRESULT DoStackSnapshot(ThreadID, StackSnapshotCallback, DWORD, void*, BYTE*, ULONG) override
    {
        return E_NOTIMPL;
    }

    HRESULT GetThreadInfo(ThreadID, DWORD*) override
    {
        return E_NOTIMPL;
    }

    HRESULT GetFunctionFromIP(LPCBYTE, FunctionID*) override
    {
        return E_NOTIMPL;
    }
};

} // namespace

TEST(StackWalkGuardTest, ConstructionIsDormant)
{
    FakeProfilerApi api;
    StackWalkGuard  guard(&api, std::chrono::seconds(1), std::chrono::seconds(1));

    ASSERT_FALSE(guard.IsStarted());
    ASSERT_TRUE(guard.IsIdle());
    ASSERT_FALSE(guard.ScheduleDssProbe());
}

TEST(StackWalkGuardTest, StartIsIdempotentAndEnablesProbes)
{
    FakeProfilerApi api;
    StackWalkGuard  guard(&api, std::chrono::seconds(1), std::chrono::seconds(1));

    ASSERT_TRUE(guard.Start());
    ASSERT_TRUE(guard.IsStarted());
    ASSERT_TRUE(guard.Start());

    ASSERT_TRUE(guard.ScheduleDssProbe());
    ASSERT_EQ(StackWalkGuard::ProbeResult::Success, guard.AwaitProbeResult());
}

TEST(StackWalkGuardTest, StopIsTerminalAndIdempotent)
{
    FakeProfilerApi api;
    StackWalkGuard  guard(&api, std::chrono::seconds(1), std::chrono::seconds(1));

    ASSERT_TRUE(guard.Start());

    guard.Stop();
    guard.Stop();

    ASSERT_FALSE(guard.IsStarted());
    ASSERT_FALSE(guard.IsIdle());
    ASSERT_FALSE(guard.Start());
    ASSERT_FALSE(guard.ScheduleDssProbe());
}
