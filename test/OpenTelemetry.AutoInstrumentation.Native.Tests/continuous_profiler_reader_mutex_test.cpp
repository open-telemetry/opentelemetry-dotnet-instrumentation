// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#ifdef _WIN32
#include <future>
#include <memory>
#include <string>

namespace
{
using MutexHandle = std::unique_ptr<void, decltype(&::CloseHandle)>;

DWORD TryAcquireOnAnotherThread(HANDLE mutex)
{
    return std::async(std::launch::async,
                      [mutex]()
                      {
                          const auto result = ::WaitForSingleObject(mutex, 0);
                          if (result == WAIT_OBJECT_0 || result == WAIT_ABANDONED)
                          {
                              ::ReleaseMutex(mutex);
                          }
                          return result;
                      })
        .get();
}
} // namespace

TEST(ContinuousProfilerReaderMutexTest, DuplicatedHandlesShareOwnershipAndSurviveOtherReadersClosing)
{
    MutexHandle first(GetContinuousProfilerReaderMutex(), &::CloseHandle);
    MutexHandle second(GetContinuousProfilerReaderMutex(), &::CloseHandle);
    ASSERT_NE(nullptr, first.get());
    ASSERT_NE(nullptr, second.get());
    ASSERT_NE(first.get(), second.get());

    DWORD flags = 0;
    ASSERT_TRUE(::GetHandleInformation(first.get(), &flags));
    EXPECT_EQ(0u, flags & HANDLE_FLAG_INHERIT);

    ASSERT_EQ(WAIT_OBJECT_0, ::WaitForSingleObject(first.get(), 0));
    EXPECT_EQ(WAIT_TIMEOUT, TryAcquireOnAnotherThread(second.get()));
    ASSERT_TRUE(::ReleaseMutex(first.get()));
    first.reset();

    EXPECT_EQ(WAIT_OBJECT_0, TryAcquireOnAnotherThread(second.get()));
    second.reset();

    MutexHandle replacement(GetContinuousProfilerReaderMutex(), &::CloseHandle);
    ASSERT_NE(nullptr, replacement.get());
    EXPECT_EQ(WAIT_OBJECT_0, TryAcquireOnAnotherThread(replacement.get()));
}

TEST(ContinuousProfilerReaderMutexTest, PredictableSessionMutexCannotBlockReader)
{
    const auto name = WStr("Local\\OpenTelemetry.AutoInstrumentation.ContinuousProfiler.Reader.") +
                      std::to_wstring(::GetCurrentProcessId());
    MutexHandle squatter(::CreateMutexW(nullptr, FALSE, name.c_str()), &::CloseHandle);
    ASSERT_NE(nullptr, squatter.get());
    ASSERT_EQ(WAIT_OBJECT_0, ::WaitForSingleObject(squatter.get(), 0));
    EXPECT_EQ(WAIT_TIMEOUT, TryAcquireOnAnotherThread(squatter.get()));

    MutexHandle reader(GetContinuousProfilerReaderMutex(), &::CloseHandle);
    ASSERT_NE(nullptr, reader.get());
    // Use a different thread because mutex acquisition is recursive for the owning thread.
    EXPECT_EQ(WAIT_OBJECT_0, TryAcquireOnAnotherThread(reader.get()));
    EXPECT_TRUE(::ReleaseMutex(squatter.get()));
}
#endif
