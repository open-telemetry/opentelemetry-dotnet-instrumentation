// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "stack_walk_guard.h"

#if defined(_WIN32)

#if defined(_M_AMD64)
// Only the RTL frame-0 path consults the symbol-resolver cache; gate
// the include to keep x86 builds free of the resolver's x64-only TU.
#include "native_symbol_resolver_impl.h"
#endif

#include <vector>

namespace ProfilerStackCapture
{

const char* StackWalkGuard::ProbeResultName(ProbeResult result) noexcept
{
    switch (result)
    {
        case ProbeResult::Success:
            return "Success";
        case ProbeResult::Failed:
            return "Failed";
        case ProbeResult::Stopping:
            return "Stopping";
        case ProbeResult::HeapOrStlLockTimeout:
            return "HeapOrStlLockTimeout";
        case ProbeResult::CanaryDssTimeout:
            return "CanaryDssTimeout";
#if defined(_M_AMD64)
        case ProbeResult::RtlLookupTimeout:
            return "RtlLookupTimeout";
        case ProbeResult::ClrIpMapTimeout:
            return "ClrIpMapTimeout";
        case ProbeResult::LoaderLockTimeout:
            return "LoaderLockTimeout";
        case ProbeResult::RtlUnwindTimeout:
            return "RtlUnwindTimeout";
#endif
        default:
            return "Unknown";
    }
}

StackWalkGuard::ProbeResult StackWalkGuard::TimeoutResultForStage(ProbeStage stage) noexcept
{
    switch (stage)
    {
        case ProbeStage::HeapOrStlGate:
            return ProbeResult::HeapOrStlLockTimeout;
        case ProbeStage::CanaryDss:
            return ProbeResult::CanaryDssTimeout;
#if defined(_M_AMD64)
        case ProbeStage::RtlLookup:
            return ProbeResult::RtlLookupTimeout;
        case ProbeStage::ClrIpMap:
            return ProbeResult::ClrIpMapTimeout;
        case ProbeStage::LoaderLock:
            return ProbeResult::LoaderLockTimeout;
        case ProbeStage::RtlUnwind:
            return ProbeResult::RtlUnwindTimeout;
#endif
        case ProbeStage::None:
        default:
            return ProbeResult::Failed;
    }
}

StackWalkGuard::StackWalkGuard(IProfilerApi*             profilerApi,
                               std::chrono::milliseconds park_timeout,
                               std::chrono::milliseconds probe_timeout)
    : api_(profilerApi), park_timeout_(park_timeout), probe_timeout_(probe_timeout)
{
    worker_ = std::make_unique<std::thread>([this]() { WorkerLoop(); });
}

StackWalkGuard::~StackWalkGuard()
{
    {
        std::lock_guard<std::mutex> lk(mutex_);
        state_ = State::Stopping;
    }
    cv_.notify_all();
    if (worker_ && worker_->joinable())
        worker_->join();
    worker_.reset();
}

bool StackWalkGuard::IsIdle() const noexcept
{
    std::lock_guard<std::mutex> lk(mutex_);
    return state_ == State::Idle;
}

bool StackWalkGuard::Schedule(const ProbeRequest& req)
{
    std::unique_lock<std::mutex> lk(mutex_);

    // Wait for the worker to be Idle. Idle covers both "fresh" and
    // "previous verdict published but not yet consumed" - either way the
    // worker is quiescent and any stale result_ is about to be overwritten
    // below. If the worker is still Running (draining an abandoned previous
    // round), park until it reaches Idle or park_timeout_ elapses.
    const bool ready =
        cv_.wait_for(lk, park_timeout_, [this] { return state_ == State::Idle || state_ == State::Stopping; });

    if (!ready || state_ == State::Stopping)
    {
        return false;
    }

    // Latch the request by value under mutex_. ProbeRequest is a
    // trivially-copyable POD (scalars + borrowed pointers + two embedded
    // structs on x64); no allocation happens on this (suspending) thread.
    req_     = req;
    abandon_ = false;
    result_  = ProbeResult::Failed;
    // Reset the stage breadcrumb so a stale stage from a previous abandoned
    // round cannot leak into the next timeout classification.
    active_stage_.store(ProbeStage::None, std::memory_order_relaxed);
    state_ = State::Scheduled;
    cv_.notify_one();
    return true;
}

bool StackWalkGuard::ScheduleDssProbe(ThreadID canary)
{
    ProbeRequest req;
    req.kind   = ProbeKind::CanaryDss;
    req.canary = canary;
    return Schedule(req);
}

#if defined(_M_AMD64)
bool StackWalkGuard::ScheduleRtlFrame0Probe(CONTEXT* ctx, continuous_profiler::CapturedFrame* frame)
{
    if (ctx == nullptr || frame == nullptr)
        return false;

    ProbeRequest req;
    req.kind      = ProbeKind::RtlFrame0;
    req.ctx_inout = ctx;
    req.frame_out = frame;
    // staged_ctx is populated by the worker from *ctx_inout once it starts
    // running; the caller is blocked in Await throughout, so we don't need
    // to copy *ctx here. Keeping the copy on the worker side also avoids
    // a redundant ~1.2 KB memcpy on the (suspending) scheduling thread.
    return Schedule(req);
}

StackWalkGuard::ProbeResult StackWalkGuard::AwaitRtlFrame0ProbeResult()
{
    return AwaitProbeResult();
}
#endif // defined(_M_AMD64)

StackWalkGuard::ProbeResult StackWalkGuard::AwaitProbeResult()
{
    std::unique_lock<std::mutex> lk(mutex_);

    const bool done =
        cv_.wait_for(lk, probe_timeout_, [this] { return state_ == State::Idle || state_ == State::Stopping; });

    if (state_ == State::Stopping)
    {
        abandon_ = true;
        return ProbeResult::Stopping;
    }

    if (!done)
    {
        // Timeout. Derive a precise result from the worker's last
        // published probe stage, then set the abandon flag so the worker
        // discards staged outputs when it eventually unblocks.
        const ProbeResult timeoutResult = TimeoutResultForStage(GetStage());
        abandon_                        = true;
        result_                         = timeoutResult;

        // Do NOT log or allocate here. Target is still suspended;
        // caller logs after ThreadGuard unwind resumes it.
        return timeoutResult;
    }

    const ProbeResult verdict = result_;
    cv_.notify_all();
    return verdict;
}

void StackWalkGuard::WorkerLoop()
{
    for (;;)
    {
        ProbeRequest req;

        // Wait for a new request (or shutdown).
        {
            std::unique_lock<std::mutex> lk(mutex_);
            cv_.wait(lk, [this] { return state_ == State::Scheduled || state_ == State::Stopping; });

            if (state_ == State::Stopping)
                break;

            req      = req_; // POD copy under lock; no allocation
            abandon_ = false;
            state_   = State::Running;
        }

        // COMPULSORY STL gate runs HERE, in WorkerLoop, NOT inside the
        // per-probe RunXxxChecks functions. Two reasons:
        //   1) MSVC C2712: a function that uses __try / __except cannot
        //      also contain auto objects with non-trivial destructors.
        //      RunRtlFrame0Checks (x64) wraps RtlLookupFunctionEntry,
        //      GetFunctionFromIP, GetModuleFileNameW, and RtlVirtualUnwind
        //      in SEH; keeping std::vector OUT of that function lets it
        //      stay pure POD + SEH-safe.
        //   2) Single source of truth: certifying the heap CS + STL
        //      iterator/debug-list CS once per round (here) eliminates
        //      duplication across CanaryDss (all archs) and RtlFrame0
        //      (x64 only) paths.
        // Cooperative: if the orchestrator abandoned us while we were
        // blocked on the heap CS, bail before dispatching the probe.
        SetStage(ProbeStage::HeapOrStlGate);
        try
        {
            std::vector<int> v{1, 2, 3};
        }
        catch (...)
        {
            {
                std::lock_guard<std::mutex> lk(mutex_);
                result_ = ProbeResult::Failed;
                state_  = State::Idle;
                SetStage(ProbeStage::None);
            }
            cv_.notify_all();
            continue;
        }

        bool gateOk;
        {
            std::lock_guard<std::mutex> lk(mutex_);
            gateOk = !abandon_;
        }

        // Dispatch to the request-specific probe iff the STL gate passed.
        // RunChecks may STAGE outputs into req.staged_* (RtlFrame0).
        const bool ok = gateOk && RunChecks(req);

        // Publish verdict and return to Idle. The commit of staged
        // outputs to caller memory happens HERE, under mutex_, gated on
        // success AND !abandon_. This is the only place caller-owned
        // *ctx_inout / *frame_out are written, and the gate guarantees
        // we never touch them for an abandoned round.
        {
            std::lock_guard<std::mutex> lk(mutex_);
            if (state_ == State::Stopping)
                break;

            const bool success = ok && !abandon_;

#if defined(_M_AMD64)
            // RtlFrame0 staged-output commit (x64 only). On x86 the
            // RtlFrame0 enum value does not exist, so this branch is
            // statically unreachable and is compiled out.
            if (success && req.kind == ProbeKind::RtlFrame0 && req.ctx_inout != nullptr && req.frame_out != nullptr)
            {
                *req.ctx_inout = req.staged_ctx;
                *req.frame_out = req.staged_frame;
            }
#endif

            result_ = success ? ProbeResult::Success : ProbeResult::Failed;
            state_  = State::Idle;
            SetStage(ProbeStage::None);
        }
        cv_.notify_all();
    }
}

bool StackWalkGuard::RunChecks(ProbeRequest& req) noexcept
{
    switch (req.kind)
    {
        case ProbeKind::CanaryDss:
            return RunCanaryChecks(req.canary);
#if defined(_M_AMD64)
        case ProbeKind::RtlFrame0:
            return RunRtlFrame0Checks(req);
#endif
        case ProbeKind::None:
        default:
            return false;
    }
}

bool StackWalkGuard::RunCanaryChecks(ThreadID canary) noexcept
{
    auto abandoned = [this]() -> bool
    {
        std::lock_guard<std::mutex> lk(mutex_);
        return abandon_;
    };

    // STL gate already certified by WorkerLoop before dispatch.

    // Process-wide DSS health (optional). canary == 0 reduces to an
    // STL-only round (gate already done) - nothing further to verify.
    if (canary == 0)
        return true;

    SetStage(ProbeStage::CanaryDss);

    StackSnapshotCallbackContext sink{};
    bool                         fired = false;
    sink.callback                      = [&fired](StackSnapshotCallbackContext*) -> HRESULT
    {
        fired = true;
        return S_FALSE;
    };

    HRESULT hr = E_FAIL;
    try
    {
        hr = api_->DoStackSnapshotUnseeded(canary, &sink);
    }
    catch (...)
    {
        return false;
    }

    if (!fired || hr != CORPROF_E_STACKSNAPSHOT_ABORTED)
        return false;
    if (abandoned())
        return false;

    return true;
}

#if defined(_M_AMD64)
// ---------------------------------------------------------------------------
// RTL Frame-0 Probe: Zero-Waste Safety Gate
// ---------------------------------------------------------------------------
//
// This probe is not a dry run. It is simultaneously a deadlock-safety proof
// and a productive computation. Upon success it commits two outputs:
//
//   staged_frame  - Fully composed frame-0 (classified IP, functionId,
//                   managed/native verdict). Ready to emit via callback.
//   staged_ctx    - CONTEXT unwound to frame-1. Ready to feed directly
//                   into WalkNativeUntilManaged as the continuation seed.
//
// A naive probe-then-redo pattern would execute each hazardous call twice:
//
//   Probe pass:  RtlLookup + GetFunctionFromIP + GetModuleFileNameW + Unwind
//   Work pass:   RtlLookup + GetFunctionFromIP + GetModuleFileNameW + Unwind
//
// This design collapses both into one pass. Each call serves dual purpose:
//
//   RtlLookupFunctionEntry  - proves target does not hold RTL CS
//                             AND yields the RUNTIME_FUNCTION for unwind.
//   GetFunctionFromIP       - proves target does not hold CLR JIT/IP-map CS
//                             AND classifies the frame as managed or native.
//   GetModuleFileNameW      - proves target does not hold loader lock
//                             AND provides the path for system classification.
//   RtlVirtualUnwind        - proves unwind metadata is not corrupted by a
//                             CS-held update AND advances ctx to frame-1.
//
// The continuation walk (frames 1..N) is safe by induction: the probe proved
// the target does not hold any of the above CSes, and the target remains
// suspended for the entire round - it cannot subsequently acquire any CS.
// Therefore calls to the same APIs on the caller thread for frames 1..N
// cannot deadlock against the target.
//
// The staging area (ProbeRequest::staged_ctx / staged_frame) exists precisely
// because the probe produces real outputs that must survive the mutex-guarded
// commit step. The orchestrator publishes staged outputs to caller memory
// under lock, only on success and only when not abandoned.
// ---------------------------------------------------------------------------
bool StackWalkGuard::RunRtlFrame0Checks(ProbeRequest& req) noexcept
{
    // This function MUST remain free of auto objects with non-trivial
    // destructors (MSVC C2712). The 'abandoned' lambda below captures
    // [this] only - trivially destructible - so it does not taint the
    // function's unwind requirements. STL-allocating helpers (gate,
    // logging that materializes std::string in our frame, etc.) must
    // NOT be added here; keep them in WorkerLoop or inside helper fns.
    //
    // Entire function is gated on _M_AMD64: RtlVirtualUnwind /
    // RtlLookupFunctionEntry have no x86 equivalent, and on x86 the
    // ProbeKind::RtlFrame0 enum value does not exist so this function
    // is never reachable.
    auto abandoned = [this]() -> bool
    {
        std::lock_guard<std::mutex> lk(mutex_);
        return abandon_;
    };

    if (req.ctx_inout == nullptr || req.frame_out == nullptr)
        return false;

    // Snapshot the caller's CONTEXT once into staging. From here on we
    // operate exclusively on req.staged_ctx; RtlVirtualUnwind mutates in
    // place, and the result is committed to *req.ctx_inout by the
    // orchestrator under lock only if we succeed and the round is not
    // abandoned.
    req.staged_ctx = *req.ctx_inout;

    const DWORD64 frame0Rip = req.staged_ctx.Rip;
    if (frame0Rip == 0)
        return false;

    // Step 1: RtlLookupFunctionEntry. May block on loader CS / RTL
    // function-table CS held by the suspended target. SEH-guarded
    // because corrupt .pdata or an out-of-range IP can fault.
    SetStage(ProbeStage::RtlLookup);
    DWORD64              imageBase    = 0;
    UNWIND_HISTORY_TABLE historyTable = {};
    PRUNTIME_FUNCTION    rtFunc       = nullptr;
    __try
    {
        rtFunc = RtlLookupFunctionEntry(frame0Rip, &imageBase, &historyTable);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }

    // Cooperative checkpoint: if the lookup blocked on a CS that the
    // orchestrator considers hazardous, it has set abandon_ during the
    // wait. Bail before touching CLR locks.
    if (abandoned())
        return false;

    // Step 2: Classify frame-0 as managed vs native via the CLR's IP
    // map. May acquire a CLR-internal CS.
    SetStage(ProbeStage::ClrIpMap);
    FunctionID managedFuncId = 0;
    HRESULT    fnHr          = E_FAIL;
    __try
    {
        fnHr = api_->GetFunctionFromIP(reinterpret_cast<LPCBYTE>(frame0Rip), &managedFuncId);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }

    // Checkpoint after JIT CS; also serves as the "before loader-lock"
    // gate so we don't pile another hazardous call onto a stale round.
    if (abandoned())
        return false;

    const bool isManaged = SUCCEEDED(fnHr) && managedFuncId != 0;

    // Step 3: Stage the decoded frame.
    req.staged_frame.frameInfo          = 0;
    req.staged_frame.contextSize        = 0;
    req.staged_frame.context            = nullptr;
    req.staged_frame.instructionPointer = static_cast<UINT_PTR>(frame0Rip);

    if (isManaged)
    {
        // Managed seed boundary. staged_ctx IS the seed - no unwind.
        // Caller will hand it to seeded DoStackSnapshot.
        req.staged_frame.functionId       = managedFuncId;
        req.staged_frame.isUnmanagedFrame = false;
    }
    else
    {
        // Native frame-0. Emit IP, then unwind staged_ctx in place so
        // the caller's fast-path RTL walk resumes from frame-1.
        req.staged_frame.functionId       = 0;
        req.staged_frame.isUnmanagedFrame = true;

        // Step 3a: Loader-lock probe + IsSystem classification.
        // GetModuleFileNameW acquires the loader lock - the SINGLE most
        // hazardous CS in the process (held by every DllMain, every
        // LoadLibrary/FreeLibrary, every loader walk). Worker bears it
        // under SEH (in case imageBase points to a partially-unloaded
        // module) and with imageBase != 0 precondition (NULL HMODULE
        // would return the EXE path - wrong classification).
        SetStage(ProbeStage::LoaderLock);
        UINT_PTR frameIp        = static_cast<UINT_PTR>(req.staged_ctx.Rip);
        bool     classified     = false;
        bool     isSystemModule = false;
        if (imageBase != 0)
        {
            WCHAR modulePath[MAX_PATH] = {};
            DWORD pathLen              = 0;
            __try
            {
                pathLen = GetModuleFileNameW(reinterpret_cast<HMODULE>(imageBase), modulePath, MAX_PATH);
                // GetModuleFileNameW may not NUL-terminate when the path
                // fills or exceeds the buffer. Unconditionally force a
                // terminator.
                modulePath[MAX_PATH - 1] = L'\0';
            }
            __except (EXCEPTION_EXECUTE_HANDLER)
            {
                pathLen = 0;
            }

            // Cooperative checkpoint AFTER the loader-lock call - it is
            // the call most likely to have blocked past probe_timeout.
            // Also serves as the "before Step 4 unwind" gate.
            if (abandoned())
                return false;

            if (pathLen != 0)
            {
                auto sysModule = NativeSymbolResolver::Instance().IsSystemModule(imageBase);
                isSystemModule = sysModule.value_or(false);
                classified     = sysModule.has_value();
            }
        }

        if (classified)
        {
            // Mirror production walker's frameIp aggregation: non-system
            // modules collapse to the image base (per-module aggregation);
            // system modules expose the function entry (per-function).
            if (!isSystemModule)
            {
                frameIp = static_cast<UINT_PTR>(imageBase);
            }
            else
            {
                frameIp = (rtFunc != nullptr) ? static_cast<UINT_PTR>(imageBase + rtFunc->BeginAddress)
                                              : static_cast<UINT_PTR>(req.staged_ctx.Rip);
            }
        }
        else if (imageBase != 0)
        {
            // Classification failed (GetModuleFileNameW returned 0 or
            // sysModule was nullopt) but we still have an image base.
            // Match production walker: prefer function-entry IP over raw
            // Rip so the frame remains symbolizable downstream.
            frameIp = (rtFunc != nullptr) ? static_cast<UINT_PTR>(imageBase + rtFunc->BeginAddress)
                                          : static_cast<UINT_PTR>(req.staged_ctx.Rip);
        }
        // else: imageBase == 0 - frameIp stays as raw Rip set above.

        req.staged_frame.instructionPointer = frameIp;

        // Step 4: Unwind one frame in place. SEH-guarded for corrupt
        // unwind data on the suspended target's stack.
        SetStage(ProbeStage::RtlUnwind);
        __try
        {
            if (rtFunc == nullptr)
            {
                if (req.staged_ctx.Rsp == 0)
                    return false;
                req.staged_ctx.Rip = *reinterpret_cast<const DWORD64*>(req.staged_ctx.Rsp);
                req.staged_ctx.Rsp += 8;
            }
            else
            {
                // strictly speaking we only need to unwind if we classified as native, but the marginal cost of an
                // extra unwind on misclassified frames is likely much lower than the cost of a second SEH block, so
                // just always unwind if we have a function entry. This fortifies our probe - we probe if
                // RtlVirtualUnwind is safe to be invoked
                void*   handlerData      = nullptr;
                DWORD64 establisherFrame = 0;
                RtlVirtualUnwind(UNW_FLAG_NHANDLER, imageBase, frame0Rip, rtFunc, &req.staged_ctx, &handlerData,
                                 &establisherFrame, nullptr);
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            return false;
        }

        if (req.staged_ctx.Rip == frame0Rip)
            return false;
    }

    // Final cooperative checkpoint before the orchestrator commits the
    // staged outputs under lock.
    if (abandoned())
        return false;

    return true;
}
#endif // defined(_M_AMD64)

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)