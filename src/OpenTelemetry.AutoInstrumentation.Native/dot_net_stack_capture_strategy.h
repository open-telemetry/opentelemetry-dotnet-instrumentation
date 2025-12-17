// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_DOTNET_STACK_CAPTURE_STRATEGY_H_
#define OTEL_PROFILER_DOTNET_STACK_CAPTURE_STRATEGY_H_

#include "stack_capture_strategy.h"

#include "logger.h"
#include <sstream>

namespace continuous_profiler {

/// @brief Stack capture strategy for .NET Core/5+
/// @details Uses SuspendRuntime/ResumeRuntime to pause entire CLR
class DotNetStackCaptureStrategy : public IStackCaptureStrategy {
public:
    explicit DotNetStackCaptureStrategy(ICorProfilerInfo12* profilerInfo)
        : profilerInfo_(profilerInfo) {
        trace::Logger::Info("Initialized DotNetStackCaptureStrategy (CLR suspension)");
    }
    
    HRESULT CaptureStacks(
        const std::unordered_set<ThreadID>& threads,
        StackSnapshotCallbackContext* clientData) override {
        
        if (threads.empty()) {
            return S_OK;
        }
        try
        {
            // RAII guard - suspends CLR in constructor, resumes in destructor
            RuntimeSuspensionGuard suspensionGuard(profilerInfo_);
            // With CLR suspended, capture stacks for requested threads
            HRESULT captureResult = S_OK;
            for (ThreadID tid : threads) {
                clientData->threadId = tid;
                HRESULT frameHr = profilerInfo_->DoStackSnapshot(
                    tid,
                    continuous_profiler::IStackCaptureStrategy::StackSnapshotCallbackDefault,
                    COR_PRF_SNAPSHOT_DEFAULT,
                    clientData,
                    nullptr,
                    0);

                if (FAILED(frameHr)) {
                    //trace::Logger::Debug("DoStackSnapshot failed for thread ", tid,
                    //    " HRESULT=", trace::HResultStr(frameHr));
                    if (SUCCEEDED(captureResult)) {
                        captureResult = frameHr; // Remember first error
                    }
                }
            }

            // RuntimeSuspensionGuard destructor will automatically resume CLR
            return SUCCEEDED(captureResult) ? S_OK : captureResult;
        }
        catch (const std::runtime_error& ex)
        {
            trace::Logger::Error("DotNetStackCaptureStrategy: Runtime Error: ", ex.what());
            return E_FAIL;
        }
        catch (const std::exception& ex) {
            trace::Logger::Error("DotNetStackCaptureStrategy: Exception during CaptureStacks: ", ex.what());
            return E_FAIL;
        }
    }
    
    // No thread tracking needed - CLR suspension is global
    
private:
    ICorProfilerInfo12* profilerInfo_;
    
    /// @brief RAII guard for CLR runtime suspension/resumption
    class RuntimeSuspensionGuard {
    public:
        explicit RuntimeSuspensionGuard(ICorProfilerInfo12* profilerInfo)
            : profilerInfo_(profilerInfo) {  // Initialize member
            
            if (auto suspendResult = profilerInfo_->SuspendRuntime(); FAILED(suspendResult))
            {
                auto errorString = "SuspendRuntime failed with HRESULT=" + std::to_string(suspendResult);
                throw std::runtime_error(errorString);
            }
        }

        ~RuntimeSuspensionGuard() {
            
            if (HRESULT resumeHr = profilerInfo_->ResumeRuntime(); FAILED(resumeHr)) {
                trace::Logger::Error("DotNetStackCaptureStrategy: ResumeRuntime FAILED! HRESULT=", 
                                    trace::HResultStr(resumeHr));
            }
        }
        
        // Non-copyable, non-movable
        RuntimeSuspensionGuard(const RuntimeSuspensionGuard&) = delete;
        RuntimeSuspensionGuard& operator=(const RuntimeSuspensionGuard&) = delete;
        RuntimeSuspensionGuard(RuntimeSuspensionGuard&&) = delete;
        RuntimeSuspensionGuard& operator=(RuntimeSuspensionGuard&&) = delete;
        
    private:
        ICorProfilerInfo12* profilerInfo_;
    };
};

} // namespace continuous_profiler

#endif // OTEL_PROFILER_DOTNET_STACK_CAPTURE_STRATEGY_H_