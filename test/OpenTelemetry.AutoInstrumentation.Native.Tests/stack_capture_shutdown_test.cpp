#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/runtime_capture.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/stack_capturer.h"

using namespace continuous_profiler;

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

    int deliveredFrames = 0;

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
        return ProfilerStackCapture::StackSnapshotCallbackDefault(0, 0, 0, 0, nullptr, context);
    }

    int  captureCalls = 0;
    bool resumed      = false;
};

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
    EXPECT_TRUE(stacks.empty());
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
}

TEST(StackCapturerTest, NativeFrameCancellationIsRecordedByTheSharedCallbackContext)
{
    ProfilerStackCapture::StackSnapshotCallbackContext context{[](ProfilerStackCapture::StackSnapshotCallbackContext*)
                                                               { return S_FALSE; }};
    context.frame.isUnmanagedFrame = true;

    EXPECT_EQ(S_FALSE, context.InvokeCallback());
    EXPECT_TRUE(context.cancellationRequested);
}
