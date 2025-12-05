// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#ifndef OTEL_PROFILER_STACK_CAPTURE_H_
#define OTEL_PROFILER_STACK_CAPTURE_H_

#include <windows.h>
#include <vector>
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

namespace ProfilerStackCapture {

    // Forward declarations
    class ILogger;

    /// @brief Log severity levels for diagnostic output
    enum class LogLevel {
        Trace,      // Detailed trace information
        Debug,      // Debug-level messages
        Info,       // Informational messages
        Warning,    // Warning conditions
        Error,      // Error conditions
        Critical    // Critical failures
    };

    /// @brief Logger interface that can be replaced by consumer's logging framework
    /// @note Default implementation uses OutputDebugString, but consumers should provide
    ///       their own implementation for production use
    class ILogger {
    public:
        virtual ~ILogger() = default;
        
        /// @brief Log a message with specified severity
        /// @param level Severity level
        /// @param message Message to log
        virtual void Log(LogLevel level, const char* message) = 0;
        
        /// @brief Check if a log level is enabled
        /// @param level Level to check
        /// @return true if logging is enabled for this level
        virtual bool IsEnabled(LogLevel level) const = 0;
    };

    /// @brief Default logger implementation using OutputDebugString
  
    /// @brief Rich error context for diagnostic purposes
    struct ErrorContext {
        HRESULT hresult;              // COM error code
        DWORD win32Error;             // GetLastError() value
        std::string operation;        // Operation that failed
        std::string details;          // Additional diagnostic information
        
        ErrorContext() 
            : hresult(S_OK), win32Error(0) {}
        
        ErrorContext(HRESULT hr, const std::string& op, const std::string& det = "") 
            : hresult(hr), win32Error(GetLastError()), operation(op), details(det) {}
        
        ErrorContext(HRESULT hr, DWORD lastErr, const std::string& op, const std::string& det = "")
            : hresult(hr), win32Error(lastErr), operation(op), details(det) {}
        
        /// @brief Format error context as human-readable string
        std::string ToString() const;
        
        /// @brief Check if this represents an error condition
        bool IsError() const { return FAILED(hresult) || win32Error != 0; }
    };

    class IThreadActivityListener
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) = 0;
        virtual HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) = 0;
        virtual HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread(ThreadID managedThreadId,
            DWORD osThreadId) = 0;
        virtual HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) = 0;


    };

    struct StackFrame {
        FunctionID functionId;
        bool isNative;
        StackFrame(FunctionID id, bool native = false) : functionId(id), isNative(native) {}
    };

    struct CaptureOptions {
        std::chrono::milliseconds probeTimeout = std::chrono::milliseconds(250);
        std::chrono::milliseconds snapshotInterval = std::chrono::milliseconds(1000);
        size_t maxStackDepth = 10000;
        const wchar_t* canaryThreadNamePrefix = L"OpenTelemetry Continuous";
    };

    class IProfilerApi {
    public:
        virtual ~IProfilerApi() = default;
        virtual HRESULT DoStackSnapshot(
            ThreadID threadId,
            StackSnapshotCallback callback,
            DWORD infoFlags,
            void* clientData,
            BYTE* context,
            ULONG contextSize) = 0;
        virtual HRESULT GetFunctionFromIP(LPCBYTE ip, FunctionID* functionId) = 0;
    };

    class ProfilerApiAdapter : public IProfilerApi {
    private:
        ICorProfilerInfo2* profilerInfo_;
    public:
        explicit ProfilerApiAdapter(ICorProfilerInfo2* profilerInfo) : profilerInfo_(profilerInfo) {}
        HRESULT DoStackSnapshot(ThreadID threadId, StackSnapshotCallback callback, DWORD infoFlags, void* clientData, BYTE* context, ULONG contextSize) override;
        HRESULT GetFunctionFromIP(LPCBYTE ip, FunctionID* functionId) override;
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

    class ScopedThreadSuspend {
    public:
        explicit ScopedThreadSuspend(DWORD nativeThreadId);
        ~ScopedThreadSuspend();
        ScopedThreadSuspend(const ScopedThreadSuspend&) = delete;
        ScopedThreadSuspend& operator=(const ScopedThreadSuspend&) = delete;
        ScopedThreadSuspend(ScopedThreadSuspend&& other) noexcept;
        ScopedThreadSuspend& operator=(ScopedThreadSuspend&& other) noexcept;
        HANDLE GetHandle() const { return threadHandle_; }
        bool IsValid() const { return threadHandle_ != INVALID_HANDLE_VALUE; }
    private:
        HANDLE threadHandle_;
        bool suspended_;
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
        std::wstring name;
        void         reset()
        {
            managedId = 0;
            nativeId  = 0;
            name.clear();
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
        continuous_profiler::StackSnapshotCallbackParams* clientParams = nullptr;
    };

    class StackCaptureEngine : public IThreadActivityListener {
    public:
        explicit StackCaptureEngine(std::unique_ptr<IProfilerApi> profilerApi, const CaptureOptions& options = {});
        ~StackCaptureEngine();
        bool    WaitForCanaryThread(std::chrono::milliseconds timeout = std::chrono::milliseconds(0));
        HRESULT CaptureStacks(const std::unordered_set<ThreadID> &threads, continuous_profiler::StackSnapshotCallbackParams* clientData);
        void Stop();
        
        HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) override { return S_OK; }
        HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) override;
        HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override;
        HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
        
    private:
        HRESULT CaptureStackSeeded(ThreadID managedThreadId, HANDLE threadHandle, StackCaptureContext* stackCaptureContext);
        bool IsManagedFunction(BYTE* instructionPointer) const;
        bool SafetyProbe();
        
        // Logging helpers
        void LogTrace(const char* format, ...);
        void LogDebug(const char* format, ...);
        void LogInfo(const char* format, ...);
        void LogWarning(const char* format, ...);
        void LogError(const char* format, ...);
        void LogCritical(const char* format, ...);
        void LogError(const ErrorContext& errorCtx);
        
        std::unique_ptr<IProfilerApi> profilerApi_;
        CaptureOptions options_;
        std::atomic<bool> stopRequested_{};
        std::shared_ptr<ILogger> logger_;
        
        // Single mutex protects all thread-related data (activeThreads_, threadNames_, canaryThread_)
        mutable std::mutex threadListMutex_;
        std::map<ThreadID, DWORD> activeThreads_;
        std::map<ThreadID, std::wstring> threadNames_;
        CanaryThreadInfo canaryThread_;  // Protected by threadListMutex_
        
        std::condition_variable captureCondVar_;
        std::unique_ptr<InvocationQueue> invocationQueue_;
    };

} // namespace ProfilerStackCapture
#endif // OTEL_PROFILER_STACK_CAPTURE_H_