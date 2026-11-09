// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#define NOMINMAX
#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/stack_capture_types.h"

#include <algorithm>
#include <array>
#include <functional>

using namespace continuous_profiler;

namespace
{

// Only the two COM methods exercised by these tests are populated. ICorProfilerInfo2 adds
// DoStackSnapshot at slot 36 and GetFunctionInfo2 at slot 38 on both 32-bit and 64-bit platforms.
class StackCaptureProfilerInfo
{
public:
    // Keep all data members in the same access section so this COM test double has standard layout.
    void** vtable_;

    StackCaptureProfilerInfo() : vtable_(Vtable()) {}

    ICorProfilerInfo7* GetInterface()
    {
        return reinterpret_cast<ICorProfilerInfo7*>(this);
    }

    size_t allocationFrames = 0;
    size_t lookupCalls      = 0;
    size_t deliveredFrames  = 0;

private:
    static HRESULT STDMETHODCALLTYPE GetFunctionInfo2(ICorProfilerInfo7* info,
                                                      FunctionID         functionId,
                                                      COR_PRF_FRAME_INFO,
                                                      ClassID*,
                                                      ModuleID* module,
                                                      mdToken*  token,
                                                      ULONG32,
                                                      ULONG32*,
                                                      ClassID*)
    {
        auto* self = reinterpret_cast<StackCaptureProfilerInfo*>(info);
        self->lookupCalls++;
        *module = 1;
        *token  = static_cast<mdToken>(functionId);
        return S_OK;
    }

    static HRESULT STDMETHODCALLTYPE DoStackSnapshot(
        ICorProfilerInfo7* info, ThreadID, StackSnapshotCallback callback, DWORD, void* clientData, BYTE*, ULONG32)
    {
        auto* self = reinterpret_cast<StackCaptureProfilerInfo*>(info);
        for (size_t i = 0; i < self->allocationFrames; i++)
        {
            self->deliveredFrames++;
            if (callback(i + 1, 0, 0, 0, nullptr, clientData) != S_OK)
            {
                return CORPROF_E_STACKSNAPSHOT_ABORTED;
            }
        }
        return S_OK;
    }

    static void** Vtable()
    {
        static std::array<void*, 39> methods = []
        {
            std::array<void*, 39> result{};
            result[36] = reinterpret_cast<void*>(&DoStackSnapshot);
            result[38] = reinterpret_cast<void*>(&GetFunctionInfo2);
            return result;
        }();
        return methods.data();
    }
};

enum class FrameKind
{
    Managed,
    Native,
    Mixed
};

class FrameSequenceStackWalker final : public IStackWalker
{
public:
    HRESULT CaptureStacks(const std::unordered_set<ThreadID>& threads, StackCaptureRequest* request) override
    {
        for (const auto threadId : threads)
        {
            for (size_t i = 0; i < framesPerThread; i++)
            {
                if (beforeFrame)
                {
                    beforeFrame(deliveredFrames);
                }

                CapturedFrame frame{};
                frame.threadId = threadId;
                const auto id  = threadId * framesPerThread + i + 1;
                if (kind == FrameKind::Native || (kind == FrameKind::Mixed && i % 2 == 0))
                {
                    frame.isUnmanagedFrame   = true;
                    frame.instructionPointer = id;
                }
                else
                {
                    frame.functionId = id;
                }

                if (configureFrame)
                {
                    configureFrame(frame);
                }

                deliveredFrames++;
                const auto result = request->onFrame(&frame);
                if (result != S_OK)
                {
                    return result;
                }
            }
        }
        return S_OK;
    }

    HRESULT ResolveNativeSymbolName(UINT_PTR, trace::WSTRING&) override
    {
        return S_FALSE;
    }

    void RequestShutdown() noexcept override {}
    void WaitForShutdown() noexcept override {}

    size_t                              framesPerThread = 1;
    size_t                              deliveredFrames = 0;
    FrameKind                           kind            = FrameKind::Native;
    std::function<void(size_t)>         beforeFrame;
    std::function<void(CapturedFrame&)> configureFrame;
};

class StackCaptureBufferTest : public ::testing::Test
{
protected:
    StackCaptureBufferTest() : profiler_(allocationSessions_)
    {
        profiler_.SetStackWalker(&walker_);
        profiler_.info7         = profilerInfo_.GetInterface();
        profiler_.helper.info7_ = profilerInfo_.GetInterface();
    }

    bool Capture(const std::unordered_set<ThreadID>& threads)
    {
        return CaptureFunctionIdentifiersForThreads(&profiler_, profiler_.info7, threads, stacks_);
    }

    size_t FrameCount() const
    {
        size_t count = 0;
        for (const auto& [_, frames] : stacks_.Stacks())
        {
            count += frames.size();
        }
        return count;
    }

    size_t FrameCapacity() const
    {
        size_t capacity = 0;
        for (const auto& [_, frames] : stacks_.Stacks())
        {
            capacity += frames.capacity();
        }
        return capacity;
    }

    static std::unordered_set<ThreadID> Threads(const size_t count, const ThreadID first = 1)
    {
        std::unordered_set<ThreadID> threads;
        for (size_t i = 0; i < count; i++)
        {
            threads.insert(first + i);
        }
        return threads;
    }

    ClrAllocationSamplingSessionProvider allocationSessions_{nullptr};
    FrameSequenceStackWalker             walker_;
    StackCaptureProfilerInfo             profilerInfo_;
    ContinuousProfiler                   profiler_;
    ThreadStacksBuffer                   stacks_;
};

} // namespace

TEST_F(StackCaptureBufferTest, ManagedDepthLimitSkipsLookupAndPreservesTheCapturedPrefix)
{
    walker_.kind = FrameKind::Managed;
    for (const auto depth : {ThreadStacksBuffer::MaximumFramesPerStack - 1, ThreadStacksBuffer::MaximumFramesPerStack,
                             ThreadStacksBuffer::MaximumFramesPerStack + 1})
    {
        stacks_.Clear();
        profilerInfo_.lookupCalls = 0;
        walker_.framesPerThread   = depth;
        ASSERT_TRUE(Capture({1}));
        const auto expected = std::min(depth, ThreadStacksBuffer::MaximumFramesPerStack);
        ASSERT_EQ(expected, stacks_.Stacks().at(1).size());
        EXPECT_EQ(expected, profilerInfo_.lookupCalls);
        EXPECT_EQ(1u * depth + 1, stacks_.Stacks().at(1).front().function_token);
        EXPECT_LE(FrameCapacity(), ThreadStacksBuffer::MaximumFrameCapacity);
    }
}

TEST_F(StackCaptureBufferTest, NativeAndManagedFramesShareTheDepthLimit)
{
    walker_.kind            = FrameKind::Mixed;
    walker_.framesPerThread = ThreadStacksBuffer::MaximumFramesPerStack + 17;
    walker_.beforeFrame     = [this](const size_t delivered)
    {
        if (delivered == 0)
        {
            // The real walker suspends the runtime before invoking the first frame callback.
            ASSERT_EQ(2u, stacks_.Stacks().size());
            for (const auto& [_, frames] : stacks_.Stacks())
            {
                EXPECT_TRUE(frames.empty());
                EXPECT_GT(frames.capacity(), 0u);
            }
        }
    };

    ASSERT_TRUE(Capture({1, 2}));

    for (const auto& [_, frames] : stacks_.Stacks())
    {
        ASSERT_EQ(ThreadStacksBuffer::MaximumFramesPerStack, frames.size());
        EXPECT_TRUE(frames[0].IsNative());
        EXPECT_FALSE(frames[1].IsNative());
    }
    EXPECT_EQ(2 * walker_.framesPerThread, walker_.deliveredFrames);
    EXPECT_EQ(ThreadStacksBuffer::MaximumFramesPerStack, profilerInfo_.lookupCalls);
}

TEST_F(StackCaptureBufferTest, AggregateBudgetBoundsDeepStacksAcrossManyThreads)
{
    walker_.kind            = FrameKind::Mixed;
    walker_.framesPerThread = ThreadStacksBuffer::MaximumFramesPerStack;
    const auto threadCount  = ThreadStacksBuffer::MaximumFrameCapacity / walker_.framesPerThread + 1;

    ASSERT_TRUE(Capture(Threads(threadCount)));

    EXPECT_EQ(ThreadStacksBuffer::MaximumFrameCapacity, FrameCount());
    EXPECT_LE(FrameCapacity(), ThreadStacksBuffer::MaximumFrameCapacity);
    EXPECT_EQ(threadCount * walker_.framesPerThread, walker_.deliveredFrames);
    EXPECT_EQ(FrameCount() / 2, profilerInfo_.lookupCalls);
}

TEST_F(StackCaptureBufferTest, ManyShallowStacksCannotAllocateUnboundedThreadEntries)
{
    ASSERT_TRUE(Capture(Threads(ThreadStacksBuffer::MaximumThreads * 2)));

    EXPECT_EQ(ThreadStacksBuffer::MaximumThreads, stacks_.Stacks().size());
    EXPECT_LE(FrameCapacity(), ThreadStacksBuffer::MaximumFrameCapacity);
    EXPECT_EQ(FrameCount(), stacks_.Stacks().size());
    EXPECT_EQ(ThreadStacksBuffer::MaximumThreads, FrameCount());
}

TEST_F(StackCaptureBufferTest, ResetReusesBoundedDeepStackStorage)
{
    walker_.framesPerThread = ThreadStacksBuffer::MaximumFramesPerStack;
    ASSERT_TRUE(Capture({1}));
    ASSERT_GE(FrameCapacity(), ThreadStacksBuffer::MaximumFramesPerStack);
    const auto initialCapacity = FrameCapacity();
    const auto initialStorage  = stacks_.Stacks().at(1).data();

    stacks_.Reset();

    EXPECT_EQ(initialCapacity, FrameCapacity());
    EXPECT_EQ(0u, FrameCount());
    ASSERT_TRUE(Capture({1}));
    EXPECT_EQ(initialCapacity, FrameCapacity());
    EXPECT_EQ(initialStorage, stacks_.Stacks().at(1).data());
    EXPECT_EQ(ThreadStacksBuffer::MaximumFramesPerStack, FrameCount());
}

TEST_F(StackCaptureBufferTest, BudgetPressureReclaimsCachedCapacityToAdmitMoreThreads)
{
    walker_.framesPerThread = ThreadStacksBuffer::MaximumFramesPerStack;
    ASSERT_TRUE(Capture(Threads(8)));
    stacks_.Reset();

    walker_.framesPerThread = 1;
    ASSERT_TRUE(Capture(Threads(ThreadStacksBuffer::MaximumThreads)));

    EXPECT_EQ(ThreadStacksBuffer::MaximumThreads, FrameCount());
    EXPECT_LE(FrameCapacity(), ThreadStacksBuffer::MaximumFrameCapacity);
}

TEST_F(StackCaptureBufferTest, InterleavedCallbacksKeepFramesInTheCorrectThreadStack)
{
    size_t frameId          = 0;
    walker_.framesPerThread = 4;
    walker_.configureFrame  = [&frameId](CapturedFrame& frame)
    {
        frame.threadId           = frameId % 2 + 1;
        frame.instructionPointer = ++frameId;
    };

    ASSERT_TRUE(Capture({1, 2}));

    for (ThreadID thread : {1u, 2u})
    {
        const auto& frames = stacks_.Stacks().at(thread);
        ASSERT_EQ(4u, frames.size());
        for (size_t index = 0; index < frames.size(); index++)
        {
            EXPECT_EQ(thread + index * 2, frames[index].native_ip);
        }
    }
}

TEST_F(StackCaptureBufferTest, ClearInvalidatesTheCachedStackAndReleasesTheCapacityBudget)
{
    walker_.framesPerThread = ThreadStacksBuffer::MaximumFramesPerStack;
    ASSERT_TRUE(Capture({1}));
    stacks_.Clear();
    EXPECT_EQ(nullptr, stacks_.TryPrepareFrame(1));

    walker_.framesPerThread = 1;
    ASSERT_TRUE(Capture(Threads(ThreadStacksBuffer::MaximumThreads)));

    EXPECT_EQ(ThreadStacksBuffer::MaximumThreads, FrameCount());
    EXPECT_LE(FrameCapacity(), ThreadStacksBuffer::MaximumFrameCapacity);
}

TEST_F(StackCaptureBufferTest, ReusedShallowBuffersCountAgainstTheNextCapturesBudget)
{
    const auto threads = Threads(256);
    ASSERT_TRUE(Capture(threads));
    const auto initialCapacity = FrameCapacity();
    stacks_.Reset();
    ASSERT_EQ(initialCapacity, FrameCapacity());
    ASSERT_EQ(0u, FrameCount());

    walker_.framesPerThread = ThreadStacksBuffer::MaximumFramesPerStack;
    ASSERT_TRUE(Capture(threads));

    EXPECT_LE(FrameCapacity(), ThreadStacksBuffer::MaximumFrameCapacity);
    EXPECT_LE(FrameCount(), ThreadStacksBuffer::MaximumFrameCapacity);
    EXPECT_GT(FrameCount(), threads.size());
}

TEST_F(StackCaptureBufferTest, ThreadChurnEvictsInactiveBuffersBeforeTheNextCapture)
{
    for (ThreadID first = 1; first < 10000; first += 1000)
    {
        const auto threads = Threads(400, first);
        ASSERT_TRUE(Capture(threads));
        ASSERT_EQ(threads.size(), stacks_.Stacks().size());
        EXPECT_EQ(threads.size(), FrameCount());
        EXPECT_LE(FrameCapacity(), ThreadStacksBuffer::MaximumFrameCapacity);
        for (const auto& [thread, _] : stacks_.Stacks())
        {
            EXPECT_NE(threads.end(), threads.find(thread));
        }
        stacks_.Reset();
    }
}

TEST_F(StackCaptureBufferTest, ShutdownStillCancelsAfterTheDepthLimitIsReached)
{
    walker_.framesPerThread = ThreadStacksBuffer::MaximumFramesPerStack + 2;
    walker_.beforeFrame     = [this](const size_t delivered)
    {
        if (delivered == ThreadStacksBuffer::MaximumFramesPerStack + 1)
        {
            profiler_.Shutdown();
        }
    };

    EXPECT_FALSE(Capture({1}));
    EXPECT_TRUE(stacks_.Stacks().empty());
}

TEST_F(StackCaptureBufferTest, AllocationDepthLimitPreservesTheSampleAndSkipsExcessLookups)
{
    profilerInfo_.allocationFrames = ThreadStacksBuffer::MaximumFramesPerStack + 1;
    std::vector<FunctionIdentifier> stack;

    ASSERT_TRUE(CaptureAllocationStack(&profiler_, profiler_.GetShutdownToken(), stack));

    EXPECT_EQ(ThreadStacksBuffer::MaximumFramesPerStack, stack.size());
    EXPECT_LE(stack.capacity(), ThreadStacksBuffer::MaximumFramesPerStack);
    EXPECT_EQ(ThreadStacksBuffer::MaximumFramesPerStack, profilerInfo_.lookupCalls);
    EXPECT_EQ(profilerInfo_.allocationFrames, profilerInfo_.deliveredFrames);
}
