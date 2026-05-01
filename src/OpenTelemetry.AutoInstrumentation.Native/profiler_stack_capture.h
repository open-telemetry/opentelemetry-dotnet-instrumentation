// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#ifndef OTEL_PROFILER_STACK_CAPTURE_H_
#define OTEL_PROFILER_STACK_CAPTURE_H_

#if defined(_WIN32)

#include <functional>
#include <memory>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <chrono>
#include <atomic>
#include <map>
#include <deque>
#include <future>
#include <string>
#include <unordered_set>
#include <corhlpr.h>
#include <corprof.h>
#include "stack_capture_strategy.h"
#include "profiler_api.h"
#include "native_stack_walker.h"

namespace ProfilerStackCapture {

    class IThreadActivityListener
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) = 0;
        virtual HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) = 0;
        virtual HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread(ThreadID managedThreadId,
            DWORD osThreadId) = 0;
        virtual HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) = 0;
    };

    struct CaptureOptions {
        std::chrono::milliseconds probeTimeout = std::chrono::milliseconds(250);
        const wchar_t* canaryThreadName = L"OpenTelemetry Profiler Canary Thread";
        bool                      IsCanaryThread(const std::wstring& threadName) const {
            return threadName.find(canaryThreadName) == 0;
        }
    };

    struct ThreadJoinerOnDelete { 
        void operator()(std::thread* t) const { 
            if (t) { 
                if (t->joinable()) {
                    t->join(); 
                }
                delete t; 
            } 
        } 
    };

    enum class InvocationStatus { Invoked, TimedOut };

    class InvocationQueue {
    public:
        InvocationQueue();
        ~InvocationQueue();
        InvocationQueue(const InvocationQueue&) = delete;
        InvocationQueue& operator=(const InvocationQueue&) = delete;
        InvocationStatus Invoke(const std::function<void()>& fn, std::chrono::milliseconds timeout);
        void Stop();
    private:
        struct QueuedInvocation { 
            std::function<void()> fn; 
            std::promise<void> completedPromise; 
        };
        std::deque<std::shared_ptr<QueuedInvocation>> queue_;
        std::mutex mutex_;
        std::condition_variable condVar_;
        std::atomic<bool> stop_{false};
        std::unique_ptr<std::thread, ThreadJoinerOnDelete> worker_;
        void WorkerLoop();
    };

    struct CanaryThreadInfo
    {
        ThreadID     managedId = 0;
        DWORD        nativeId  = 0;
        void         reset()
        {
            managedId = 0;
            nativeId  = 0;
        }
        bool isValid() const
        {
            return managedId != 0 && nativeId != 0;
        }
    };
    struct StackCaptureContext
    {
        size_t                   maxDepth;
        std::atomic<bool>* stopRequested;
        void* clientParams = nullptr;
    };

    class StackCaptureEngine : public IThreadActivityListener {
    public:
        explicit StackCaptureEngine(std::unique_ptr<ProfilerStackCapture::IProfilerApi> profilerApi,
                                    const CaptureOptions&                             options = {},
                                    std::unique_ptr<INativeStackWalker>                 nativeWalker = nullptr);
        ~StackCaptureEngine();
        CanaryThreadInfo    WaitForCanaryThread(std::chrono::milliseconds timeout = std::chrono::milliseconds(0));
        HRESULT CaptureStacks(const std::unordered_set<ThreadID> &threads, void* clientData);
        void Stop();
        
        HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) override { return S_OK; }
        HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) override;
        HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override;
        HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
        
    private:
        HRESULT CaptureStack(ThreadID managedThreadId, StackCaptureContext* stackCaptureContext);
        HRESULT CaptureStackUnseeded(ThreadID managedThreadId, StackCaptureContext* stackCaptureContext);
        bool SafetyProbe(const CanaryThreadInfo& canaryInfo);
        
        std::unique_ptr<IProfilerApi> profilerApi_;
        CaptureOptions options_;
        std::atomic<bool> stopRequested_{};
        
        // Single mutex protects all thread-related data (activeThreads_, threadNames_, canaryThread_)
        mutable std::mutex threadListMutex_;
        std::map<ThreadID, DWORD> activeThreads_;
        std::map<ThreadID, std::wstring> threadNames_;
        CanaryThreadInfo canaryThread_;  // Protected by threadListMutex_
        
        std::condition_variable captureCondVar_;
        std::unique_ptr<InvocationQueue> invocationQueue_;
        std::unique_ptr<INativeStackWalker> nativeWalker_;
    };

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)
#endif // OTEL_PROFILER_STACK_CAPTURE_H_