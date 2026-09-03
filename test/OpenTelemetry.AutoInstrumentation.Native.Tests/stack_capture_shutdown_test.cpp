#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/runtime_capture.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/stack_capturer.h"
#if defined(_WIN32)
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/stack_walk_guard.h"
#endif

#include <chrono>
#include <condition_variable>
#include <functional>
#include <future>
#include <mutex>

using namespace continuous_profiler;

#if defined(_WIN32)
namespace ProfilerStackCapture
{

class StackWalkGuardTestPeer
{
public:
    static void ParkWorker(StackWalkGuard& guard)
    {
        guard.RequestShutdown();
        guard.worker_->join();
        guard.worker_.reset();

        std::lock_guard<std::mutex> lock(guard.mutex_);
        guard.state_   = StackWalkGuard::State::Idle;
        guard.abandon_ = false;
    }

    static StackWalkGuard::ProbeResult RunWorkerUntilIdle(StackWalkGuard& guard)
    {
        guard.worker_ = std::make_unique<std::thread>([&guard]() { guard.WorkerLoop(); });
        std::unique_lock<std::mutex> lock(guard.mutex_);
        if (!guard.cv_.wait_for(lock, std::chrono::seconds(1),
                                [&guard]() { return guard.state_ == StackWalkGuard::State::Idle; }))
        {
            return StackWalkGuard::ProbeResult::Stopping;
        }

        return guard.result_;
    }
};

} // namespace ProfilerStackCapture
#endif

namespace
{

enum class MidCaptureAction
{
    Pause,
    Shutdown
};

class TwoFrameStackWalker final : public IStackWalker
{
public:
    TwoFrameStackWalker(ContinuousProfiler* profiler, const MidCaptureAction action)
        : profiler_(profiler), action_(action)
    {
    }

    HRESULT CaptureStacks(const std::unordered_set<ThreadID>& threads, StackCaptureRequest* request) override
    {
        for (const auto threadId : threads)
        {
            for (UINT_PTR instructionPointer : {1u, 2u})
            {
                CapturedFrame frame{};
                frame.threadId           = threadId;
                frame.instructionPointer = instructionPointer;
                frame.isUnmanagedFrame   = true;

                const auto result = request->onFrame(&frame);
                deliveredFrames++;
                if (result != S_OK)
                {
                    return result;
                }

                if (deliveredFrames == 1)
                {
                    if (action_ == MidCaptureAction::Shutdown)
                    {
                        profiler_->Shutdown();
                    }
                    else
                    {
                        profiler_->StageThreadSamplingConfiguration(0, 0);
                    }
                }
            }
        }

        return S_OK;
    }

    HRESULT ResolveNativeSymbolName(UINT_PTR, trace::WSTRING&) override
    {
        return S_FALSE;
    }

    void RequestShutdown() noexcept override
    {
        stopped = true;
    }

    void WaitForShutdown() noexcept override {}

    int deliveredFrames = 0;
    bool stopped         = false;

private:
    ContinuousProfiler* profiler_;
    MidCaptureAction    action_;
};

class CallbackRuntimeCapture final : public ProfilerStackCapture::IRuntimeCapture
{
public:
    HRESULT SuspendRuntime() override
    {
        return S_OK;
    }

    void ResumeRuntime() noexcept override
    {
        resumed = true;
    }

    HRESULT CaptureStack(ThreadID, ProfilerStackCapture::StackSnapshotCallbackContext* context) override
    {
        captureCalls++;
        if (onCapture)
        {
            onCapture();
        }
        return ProfilerStackCapture::StackSnapshotCallbackDefault(0, 0, 0, 0, nullptr, context);
    }

    void RequestShutdown() noexcept override
    {
        stopped = true;
    }

    void WaitForShutdown() noexcept override
    {
        waitedForShutdown = true;
    }

    std::function<void()> onCapture;
    int  captureCalls = 0;
    bool resumed      = false;
    bool                  stopped           = false;
    bool                  waitedForShutdown = false;
};

class BlockingShutdownStackWalker final : public IStackWalker
{
public:
    HRESULT CaptureStacks(const std::unordered_set<ThreadID>&, StackCaptureRequest*) override
    {
        return S_OK;
    }

    HRESULT ResolveNativeSymbolName(UINT_PTR, trace::WSTRING&) override
    {
        return S_FALSE;
    }

    void RequestShutdown() noexcept override
    {
        std::lock_guard<std::mutex> lock(mutex_);
        stopped_ = true;
        cv_.notify_all();
    }

    void WaitForShutdown() noexcept override
    {
        std::unique_lock<std::mutex> lock(mutex_);
        waiting_ = true;
        cv_.notify_all();
        cv_.wait(lock, [this] { return released_; });
    }

    bool WaitUntilShutdownWaitBegins()
    {
        std::unique_lock<std::mutex> lock(mutex_);
        return cv_.wait_for(lock, std::chrono::seconds(1), [this] { return waiting_; });
    }

    void Release()
    {
        std::lock_guard<std::mutex> lock(mutex_);
        released_ = true;
        cv_.notify_all();
    }

    bool Stopped() const
    {
        std::lock_guard<std::mutex> lock(mutex_);
        return stopped_;
    }

private:
    mutable std::mutex      mutex_;
    std::condition_variable cv_;
    bool                    stopped_  = false;
    bool                    waiting_  = false;
    bool                    released_ = false;
};

#if defined(_WIN32)
class BlockingProfilerApi final : public ProfilerStackCapture::IProfilerApi
{
public:
    HRESULT DoStackSnapshot(ThreadID, StackSnapshotCallback, DWORD, void*, BYTE*, ULONG) override
    {
        std::unique_lock<std::mutex> lock(mutex_);
        probeStarted_ = true;
        cv_.notify_all();
        cv_.wait(lock, [this] { return released_; });
        return E_ABORT;
    }

    HRESULT GetThreadInfo(ThreadID, DWORD*) override
    {
        return E_NOTIMPL;
    }

    HRESULT GetFunctionFromIP(LPCBYTE, FunctionID*) override
    {
        return E_NOTIMPL;
    }

    void WaitUntilProbeStarts()
    {
        std::unique_lock<std::mutex> lock(mutex_);
        cv_.wait(lock, [this] { return probeStarted_; });
    }

    void Release()
    {
        std::lock_guard<std::mutex> lock(mutex_);
        released_ = true;
        cv_.notify_all();
    }

private:
    std::mutex              mutex_;
    std::condition_variable cv_;
    bool                    probeStarted_ = false;
    bool                    released_     = false;
};

#endif

} // namespace

TEST(ContinuousProfilerCaptureTest, PauseFinishesTheAdmittedCohortForPublication)
{
    ClrAllocationSamplingSessionProvider allocationSessions(nullptr);
    ContinuousProfiler                   profiler(allocationSessions);
    TwoFrameStackWalker                  walker(&profiler, MidCaptureAction::Pause);
    profiler.SetStackWalker(&walker);
    profiler.StageThreadSamplingConfiguration(1000, 0);
    std::unordered_map<ThreadID, std::vector<FunctionIdentifier>> stacks;

    EXPECT_TRUE(CaptureFunctionIdentifiersForThreads(&profiler, nullptr, {1, 2}, stacks));

    EXPECT_EQ(4, walker.deliveredFrames);
    ASSERT_EQ(2u, stacks.size());
    EXPECT_EQ(2u, stacks.at(1).size());
    EXPECT_EQ(2u, stacks.at(2).size());
}

TEST(ContinuousProfilerCaptureTest, ShutdownAbortsAtAFrameBoundaryAndDiscardsTheCohort)
{
    ClrAllocationSamplingSessionProvider allocationSessions(nullptr);
    ContinuousProfiler                   profiler(allocationSessions);
    TwoFrameStackWalker                  walker(&profiler, MidCaptureAction::Shutdown);
    profiler.SetStackWalker(&walker);
    std::unordered_map<ThreadID, std::vector<FunctionIdentifier>> stacks;

    EXPECT_FALSE(CaptureFunctionIdentifiersForThreads(&profiler, nullptr, {1}, stacks));

    EXPECT_EQ(2, walker.deliveredFrames);
    EXPECT_TRUE(walker.stopped);
    EXPECT_TRUE(stacks.empty());
}

TEST(ContinuousProfilerCaptureTest, ShutdownWaitsForTheStackWalkerWorker)
{
    ClrAllocationSamplingSessionProvider allocationSessions(nullptr);
    ContinuousProfiler                   profiler(allocationSessions);
    BlockingShutdownStackWalker          walker;
    profiler.SetStackWalker(&walker);

    auto shutdown = std::async(std::launch::async, [&profiler] { profiler.Shutdown(); });
    ASSERT_TRUE(walker.WaitUntilShutdownWaitBegins());

    EXPECT_TRUE(walker.Stopped());
    EXPECT_EQ(std::future_status::timeout, shutdown.wait_for(std::chrono::milliseconds(20)));

    walker.Release();
    ASSERT_EQ(std::future_status::ready, shutdown.wait_for(std::chrono::seconds(1)));
    shutdown.get();
}

TEST(ContinuousProfilerCaptureTest, RuntimeAbortResultDiscardsThePartialAllocationStack)
{
    ClrAllocationSamplingSessionProvider allocationSessions(nullptr);
    ContinuousProfiler                   profiler(allocationSessions);
    std::vector<FunctionIdentifier>      stack{FunctionIdentifier::Native(1)};

    EXPECT_FALSE(FinalizeAllocationStackCapture(&profiler, CORPROF_E_STACKSNAPSHOT_ABORTED, stack));
    EXPECT_TRUE(stack.empty());
}

TEST(StackCapturerTest, FrameCancellationStopsTheThreadCohortAndResumesTheRuntime)
{
    auto  runtime    = std::make_unique<CallbackRuntimeCapture>();
    auto* runtimePtr = runtime.get();
    auto  capturer   = ProfilerStackCapture::CreateStackCapturer(nullptr, std::move(runtime));
    ProfilerStackCapture::StackSnapshotCallbackContext context{[](ProfilerStackCapture::StackSnapshotCallbackContext*)
                                                               { return S_FALSE; }};

    EXPECT_EQ(S_FALSE, capturer->CaptureStacks({1, 2}, &context));
    EXPECT_EQ(1, runtimePtr->captureCalls);
    EXPECT_TRUE(runtimePtr->resumed);
    capturer->RequestShutdown();
    EXPECT_TRUE(runtimePtr->stopped);
}

TEST(StackCapturerTest, NativeFrameCancellationIsRecordedByTheSharedCallbackContext)
{
    ProfilerStackCapture::StackSnapshotCallbackContext context{[](ProfilerStackCapture::StackSnapshotCallbackContext*)
                                                               { return S_FALSE; }};
    context.frame.isUnmanagedFrame = true;

    EXPECT_EQ(S_FALSE, context.InvokeCallback());
    EXPECT_TRUE(context.cancellationRequested);
}

TEST(StackCapturerTest, RequestShutdownClosesFutureCaptureAdmission)
{
    auto  runtime    = std::make_unique<CallbackRuntimeCapture>();
    auto* runtimePtr = runtime.get();
    auto  capturer   = ProfilerStackCapture::CreateStackCapturer(nullptr, std::move(runtime));
    ProfilerStackCapture::StackSnapshotCallbackContext context{[](ProfilerStackCapture::StackSnapshotCallbackContext*)
                                                               { return S_OK; }};

    capturer->RequestShutdown();

    EXPECT_EQ(S_FALSE, capturer->CaptureStacks({1}, &context));
    EXPECT_EQ(0, runtimePtr->captureCalls);
    EXPECT_TRUE(runtimePtr->stopped);
    capturer->WaitForShutdown();
    EXPECT_TRUE(runtimePtr->waitedForShutdown);
}

TEST(StackCapturerTest, RequestShutdownDuringCapturePreventsTheNextThread)
{
    auto  runtime    = std::make_unique<CallbackRuntimeCapture>();
    auto* runtimePtr = runtime.get();
    auto  capturer   = ProfilerStackCapture::CreateStackCapturer(nullptr, std::move(runtime));
    ProfilerStackCapture::StackSnapshotCallbackContext context{[](ProfilerStackCapture::StackSnapshotCallbackContext*)
                                                               { return S_OK; }};
    runtimePtr->onCapture = [&capturer]() { capturer->RequestShutdown(); };

    EXPECT_EQ(S_FALSE, capturer->CaptureStacks({1, 2}, &context));
    EXPECT_EQ(1, runtimePtr->captureCalls);
    EXPECT_TRUE(runtimePtr->resumed);
}

#if defined(_WIN32)
TEST(StackWalkGuardTest, RequestShutdownWakesAwaitBeforeTheProbeWorkerFinishes)
{
    BlockingProfilerApi                  profilerApi;
    ProfilerStackCapture::StackWalkGuard guard(&profilerApi, std::chrono::seconds(1), std::chrono::seconds(5));

    ASSERT_TRUE(guard.ScheduleDssProbe(1));
    auto verdict = std::async(std::launch::async, [&] { return guard.AwaitProbeResult(); });
    profilerApi.WaitUntilProbeStarts();

    guard.RequestShutdown();
    const auto stopReleasedWaiter = verdict.wait_for(std::chrono::seconds(1));
    profilerApi.Release();

    ASSERT_EQ(std::future_status::ready, stopReleasedWaiter);
    EXPECT_EQ(ProfilerStackCapture::StackWalkGuard::ProbeResult::Stopping, verdict.get());
}

TEST(StackWalkGuardTest, WaitForShutdownJoinsAnInFlightProbe)
{
    BlockingProfilerApi                  profilerApi;
    ProfilerStackCapture::StackWalkGuard guard(&profilerApi, std::chrono::seconds(1), std::chrono::seconds(5));

    ASSERT_TRUE(guard.ScheduleDssProbe(1));
    profilerApi.WaitUntilProbeStarts();

    auto shutdown = std::async(std::launch::async,
                               [&guard]
                               {
                                   guard.RequestShutdown();
                                   guard.WaitForShutdown();
                               });
    EXPECT_EQ(std::future_status::timeout, shutdown.wait_for(std::chrono::milliseconds(20)));

    profilerApi.Release();
    ASSERT_EQ(std::future_status::ready, shutdown.wait_for(std::chrono::seconds(1)));
    shutdown.get();
}

TEST(StackWalkGuardTest, TimedOutScheduledRequestIsNotRevivedByWorker)
{
    ProfilerStackCapture::StackWalkGuard guard(nullptr, std::chrono::seconds(1), std::chrono::milliseconds(0));
    ProfilerStackCapture::StackWalkGuardTestPeer::ParkWorker(guard);

    ASSERT_TRUE(guard.ScheduleDssProbe());
    EXPECT_EQ(ProfilerStackCapture::StackWalkGuard::ProbeResult::Failed, guard.AwaitProbeResult());
    EXPECT_EQ(ProfilerStackCapture::StackWalkGuard::ProbeResult::Failed,
              ProfilerStackCapture::StackWalkGuardTestPeer::RunWorkerUntilIdle(guard));
}
#endif
