// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_DOTNET_STACK_CAPTURE_STRATEGY_H_
#define OTEL_PROFILER_DOTNET_STACK_CAPTURE_STRATEGY_H_

#include <sstream>
#include "stack_capture_strategy.h"
#include "profiler_api.h"
#include "logger.h"
#include "native_stack_walker.h"

#if defined(_M_AMD64) && defined(_WIN32)
#include "rtl_stack_walk.h"
#endif

namespace continuous_profiler {

/// @brief Stack capture strategy for .NET Core/5+
/// @details Uses SuspendRuntime/ResumeRuntime to pause entire CLR
class DotNetStackCaptureStrategy : public IStackCaptureStrategy {
public:
    explicit DotNetStackCaptureStrategy(ICorProfilerInfo2* profilerInfo)
        : profilerApi_(std::make_unique<ProfilerStackCapture::ProfilerApiAdapter>(profilerInfo)), 
        nativeWalker_(ProfilerStackCapture::CreateNativeStackWalker())
    {
        trace::Logger::Info("Initialized DotNetStackCaptureStrategy (CLR suspension)");
    }
    
    HRESULT CaptureStacks(
        const std::unordered_set<ThreadID>& threads,
        void* clientData) override {
        
        if (threads.empty()) {
            return S_OK;
        }
        try
        {
            // RAII guard - suspends CLR in constructor, resumes in destructor
            RuntimeSuspensionGuard suspensionGuard(profilerApi_.get());
            // With CLR suspended, capture stacks for requested threads
            HRESULT captureResult = S_OK;
            for (ThreadID tid : threads) {
                static_cast<ProfilerStackCapture::StackSnapshotCallbackContext*>(clientData)->threadId = tid;
                HRESULT frameHr = profilerApi_->DoStackSnapshotUnseeded(tid, clientData);

                if (FAILED(frameHr) && nativeWalker_)
                {
                    frameHr = nativeWalker_->WalkThread(
                        profilerApi_.get(), tid,
                        static_cast<ProfilerStackCapture::StackSnapshotCallbackContext*>(clientData));
                }

                if (FAILED(frameHr) && SUCCEEDED(captureResult)) {
                    captureResult = frameHr;
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

    HRESULT ResolveNativeSymbolName(UINT_PTR instructionPointer, trace::WSTRING& outName) override
    {
#if defined(_M_AMD64) && defined(_WIN32)
        return ProfilerStackCapture::ResolveNativeSymbolName(instructionPointer, outName) ? S_OK : S_FALSE;
#else
        return E_NOTIMPL;
#endif
    }
    
    // No thread tracking needed - CLR suspension is global
    
private:
    std::unique_ptr<ProfilerStackCapture::IProfilerApi> profilerApi_;
    std::unique_ptr<ProfilerStackCapture::INativeStackWalker> nativeWalker_;
    
    /// @brief RAII guard for CLR runtime suspension/resumption
    class RuntimeSuspensionGuard {
    public:
        explicit RuntimeSuspensionGuard(ProfilerStackCapture::IProfilerApi* profilerApi) : profilerApi_(profilerApi)
        { // Initialize member
            
            if (auto suspendResult = profilerApi_->SuspendRuntime(); FAILED(suspendResult))
            {
                auto errorString = "SuspendRuntime failed with HRESULT=" + std::to_string(suspendResult);
                throw std::runtime_error(errorString);
            }
        }

        ~RuntimeSuspensionGuard() {
            
            if (HRESULT resumeHr = profilerApi_->ResumeRuntime(); FAILED(resumeHr))
            {
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
        ProfilerStackCapture::IProfilerApi* profilerApi_;
        
    };
};

} // namespace continuous_profiler

#endif // OTEL_PROFILER_DOTNET_STACK_CAPTURE_STRATEGY_H_