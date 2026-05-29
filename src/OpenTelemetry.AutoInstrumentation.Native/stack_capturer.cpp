// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "stack_capturer.h"

#include "clr_runtime_capture.h"
#include "runtime_capture.h"
#include "suspension_guards.h"

#if defined(_WIN32)
#include "netfx_runtime_capture.h"
#endif

#if defined(_WIN32) && defined(_M_AMD64)
#include "rtl_stack_walk.h" // NativeSymbolResolver::Instance()
#endif

namespace ProfilerStackCapture
{

/// <summary>
/// PIMPL stack capturer driven by IRuntimeCapture.
/// - CLR (.NET Core / .NET 5+): global SuspendRuntime + seedless DSS, x64 native-walk fallback.
/// - .NET Framework (Windows): per-thread suspension, canary-gated probes, x64 native-walk fallback.
/// All probe and walk machinery is owned by each IRuntimeCapture instance;
/// this class only orchestrates the batch lifecycle and dispatch.
/// </summary>
class StackCaptureImpl final : public IStackCapturer
{
    using StackSnapshotCallbackContext = ProfilerStackCapture::StackSnapshotCallbackContext;

public:
    StackCaptureImpl(std::unique_ptr<IProfilerApi> profilerApi, std::unique_ptr<IRuntimeCapture> runtime)
        : profilerApi_(std::move(profilerApi)), runtime_(std::move(runtime))
    {
    }

    ~StackCaptureImpl() override
    {
        // Order matters: runtime_ (and its SafetyProber + worker thread) must
        // be torn down before profilerApi_ - the prober lambdas reference it.
        // Member declaration order below guarantees this for the implicit dtor;
        // we just signal stop early so any wait can unblock promptly.
        if (runtime_)
        {
            runtime_->Stop();
        }
    }

    HRESULT CaptureStacks(const std::unordered_set<ThreadID>& threads, void* clientData) override
    {
        if (threads.empty())
        {
            return S_OK;
        }

        auto* callbackContext = static_cast<StackSnapshotCallbackContext*>(clientData);
        if (callbackContext == nullptr)
        {
            return E_INVALIDARG;
        }

        try
        {
            // Phase 1: bring the runtime to a walkable state.
            //   ClrRuntimeCapture  -> profiler-API SuspendRuntime
            //   NetFxRuntimeCapture -> no-op (per-thread suspension done in CaptureStack)
            RuntimeGuard runtimeGuard(runtime_.get());
            if (!runtimeGuard.IsActive())
            {
                trace::Logger::Error("[StackCaptureImpl] Runtime suspension failed.");
                return E_FAIL;
            }

            // Phase 2: per-thread capture, delegated wholesale to the runtime.
            HRESULT captureResult = S_OK;
            for (ThreadID managedThreadId : threads)
            {
                HRESULT frameHr = runtime_->CaptureStack(managedThreadId, callbackContext);

                // S_FALSE (e.g. canary skip on NetFx) is not a failure.
                if (FAILED(frameHr) && SUCCEEDED(captureResult))
                {
                    captureResult = frameHr;
                }
            }

            return SUCCEEDED(captureResult) ? S_OK : captureResult;
        }
        catch (const std::exception& ex)
        {
            trace::Logger::Error("[StackCaptureImpl] Exception during CaptureStacks: ", ex.what());
            return E_FAIL;
        }
    }

    HRESULT ResolveNativeSymbolName(UINT_PTR instructionPointer, trace::WSTRING& outName) override
    {
#if defined(_WIN32) && defined(_M_AMD64)
        return NativeSymbolResolver::Instance().Resolve(instructionPointer, outName) ? S_OK : S_FALSE;
#else
        (void)instructionPointer;
        (void)outName;
        return E_NOTIMPL;
#endif
    }

    void OnThreadCreated(ThreadID threadId) override
    {
        runtime_->OnThreadCreated(threadId);
    }

    void OnThreadDestroyed(ThreadID threadId) override
    {
        runtime_->OnThreadDestroyed(threadId);
    }

    void OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override
    {
        runtime_->OnThreadNameChanged(threadId, cchName, name);
    }

    void OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override
    {
        runtime_->OnThreadAssignedToOSThread(managedThreadId, osThreadId);
    }

private:
    // Declaration order = destruction order in reverse: runtime_ destroyed first
    // (joins its SafetyProber worker), then profilerApi_.  This keeps any
    // in-flight probe lambdas safe.
    std::unique_ptr<IProfilerApi>    profilerApi_;
    std::unique_ptr<IRuntimeCapture> runtime_;
};

// ============================================================================
// Factory
// ============================================================================

std::unique_ptr<IStackCapturer> CreateStackCapturer(ICorProfilerInfo2*               profilerInfo,
                                                    continuous_profiler::RuntimeType runtimeType)
{
    auto profilerApi = std::make_unique<ProfilerApiAdapter>(profilerInfo);

    std::unique_ptr<IRuntimeCapture> runtime;
    switch (runtimeType)
    {
        case continuous_profiler::RuntimeType::DotNetCore:
            trace::Logger::Info("[CreateStackCapturer] Initializing for .NET Core runtime.");
            runtime = std::make_unique<ClrRuntimeCapture>(profilerApi.get());
            break;

        case continuous_profiler::RuntimeType::DotNetFramework:
#if defined(_WIN32)
            trace::Logger::Info("[CreateStackCapturer] Initializing for .NET Framework runtime.");
            runtime = std::make_unique<NetFxRuntimeCapture>(profilerApi.get());
            break;
#else
            trace::Logger::Error("[CreateStackCapturer] .NET Framework runtime is only supported on Windows.");
            return nullptr;
#endif

        default:
            trace::Logger::Error("[CreateStackCapturer] Unknown runtime type.");
            return nullptr;
    }

    if (runtime == nullptr)
    {
        return nullptr;
    }

    return std::make_unique<StackCaptureImpl>(std::move(profilerApi), std::move(runtime));
}

} // namespace ProfilerStackCapture