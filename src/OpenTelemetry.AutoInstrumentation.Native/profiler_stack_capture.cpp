// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifdef _WIN64 
#include "profiler_stack_capture.h"

#include <stdexcept>
#include <algorithm>
#include <cassert>
#include <dbghelp.h>
#include <atlbase.h>
#include <atlcom.h>
#include <sstream>
#include <iomanip>
#include <cstdarg>

#ifndef DECLSPEC_IMPORT
#define DECLSPEC_IMPORT __declspec(dllimport)
#endif

extern "C" {
    DECLSPEC_IMPORT PRUNTIME_FUNCTION NTAPI RtlLookupFunctionEntry(DWORD64 ControlPc, PDWORD64 ImageBase, PUNWIND_HISTORY_TABLE HistoryTable);
    DECLSPEC_IMPORT PEXCEPTION_ROUTINE NTAPI RtlVirtualUnwind(DWORD HandlerType, DWORD64 ImageBase, DWORD64 ControlPc, PRUNTIME_FUNCTION FunctionEntry, PCONTEXT ContextRecord, PVOID* HandlerData, PDWORD64 EstablisherFrame, PKNONVOLATILE_CONTEXT_POINTERS ContextPointers);
}

namespace ProfilerStackCapture {

    // ========================================================================================
    // Error Handling and Logging Infrastructure
    // ========================================================================================

    /// @brief Helper to format GetLastError with hex and decimal
    static std::string FormatLastError(DWORD lastError) {
        std::stringstream ss;
        ss << "0x" << std::hex << std::setw(8) << std::setfill('0') << lastError 
           << " (" << std::dec << lastError << ")";
        return ss.str();
    }

    /// @brief Format HRESULT as human-readable string
    static std::string FormatHResult(HRESULT hr) {
        std::stringstream ss;
        ss << "0x" << std::hex << std::setw(8) << std::setfill('0') << hr;
        
        // Add well-known error names
        if (hr == S_OK) ss << " (S_OK)";
        else if (hr == E_FAIL) ss << " (E_FAIL)";
        else if (hr == E_ABORT) ss << " (E_ABORT)";
        else if (hr == E_INVALIDARG) ss << " (E_INVALIDARG)";
        else if (hr == CORPROF_E_STACKSNAPSHOT_UNSAFE) ss << " (CORPROF_E_STACKSNAPSHOT_UNSAFE)";
        else if (hr == CORPROF_E_STACKSNAPSHOT_ABORTED) ss << " (CORPROF_E_STACKSNAPSHOT_ABORTED)";
        
        return ss.str();
    }

    // ErrorContext implementation
    std::string ErrorContext::ToString() const {
        std::stringstream ss;
        ss << "Operation: " << operation;
        
        if (FAILED(hresult)) {
            ss << ", HRESULT: " << FormatHResult(hresult);
        }
        
        if (win32Error != 0) {
            ss << ", Win32Error: " << FormatLastError(win32Error);
        }
        
        if (!details.empty()) {
            ss << ", Details: " << details;
        }
        
        return ss.str();
    }

    
    // ========================================================================================
    // SEH-Protected Helper Functions
    // ========================================================================================
    
    /// @brief Helper function for reading return address (SEH-protected, no C++ objects)
    static bool ReadReturnAddressFromStack(DWORD64 rsp, DWORD64* pReturnAddress) {
        __try {
            *pReturnAddress = *reinterpret_cast<PULONG64>(rsp);
            return true;
        } 
        __except (EXCEPTION_EXECUTE_HANDLER) {
            return false;
        }
    }

    /// @brief Helper function for RtlVirtualUnwind (SEH-protected, no C++ objects)
    static bool SafeRtlVirtualUnwind(DWORD64 imageBase, DWORD64 controlPc, PRUNTIME_FUNCTION runtimeFunction,
                                     PCONTEXT context, PULONG64 pEstablisherFrame) {
        __try {
            PVOID handlerData = nullptr;
            ULONG64 eFrame = 0;
            RtlVirtualUnwind(0, imageBase, controlPc, runtimeFunction, context, &handlerData, &eFrame, nullptr);
            
            if (pEstablisherFrame) {
                *pEstablisherFrame = eFrame;
            }
            
            return true;
        } 
        __except (EXCEPTION_EXECUTE_HANDLER) {
            return false;
        }
    }

    /// @brief Helper for safety probe worker (SEH-protected, no std::unique_ptr)
    static HRESULT ExecuteProbeOperations(IProfilerApi* profilerApi, ThreadID canaryManagedId, const CONTEXT& canaryCtx) {
        HRESULT result = S_OK;
        
        int* testAlloc = nullptr;
        __try {

            // Test 1: Heap allocation (using new/delete instead of unique_ptr as we are inside SEH block)
            if (testAlloc = new int(42))
            {
                delete testAlloc;
                testAlloc = nullptr;
            }
            // Test 2: RTL function lookup
            UNWIND_HISTORY_TABLE historyTable = {};
            DWORD64 imageBase = 0;
            RtlLookupFunctionEntry(canaryCtx.Rip, &imageBase, &historyTable);
            
            // Test 3: DoStackSnapshot
            auto probeCallback = [](FunctionID, UINT_PTR, COR_PRF_FRAME_INFO, ULONG32, BYTE[], void*) -> HRESULT { 
                return S_FALSE; 
            };
            
            result = profilerApi->DoStackSnapshot(canaryManagedId, probeCallback, COR_PRF_SNAPSHOT_DEFAULT, nullptr, nullptr, 0);
        } 
        __except (EXCEPTION_EXECUTE_HANDLER) {
            if(testAlloc) {
                delete testAlloc;
            }
            OutputDebugStringA("[StackCapture] Exception in probe operations\n");
            return E_FAIL;
        }
        
        // If stack snapshot was aborted, treat as success for probe purposes, as we explicitly
        // short-circuited it from the callback, prompting CORPROF_E_STACKSNAPSHOT_ABORTED
        return result == CORPROF_E_STACKSNAPSHOT_ABORTED ? S_OK : result;
    }

    // PrepareContextForSnapshot - walks native stack to find managed frame and prepares context for DoStackSnapshot
    static HRESULT PrepareContextForSnapshot(ThreadID managedThreadId, HANDLE threadHandle, CONTEXT* pContext, 
                                             IProfilerApi* profilerApi, std::atomic<bool>* pStopRequested) {
        char startLog[256];
        sprintf_s(startLog, "[StackCapture] PrepareContextForSnapshot START - ManagedID=%lld, Initial RIP=0x%016llX\n",
                 static_cast<long long>(managedThreadId),
                 static_cast<unsigned long long>(pContext->Rip));
        OutputDebugStringA(startLog);
        
        const int MAX_WALK_EVER = 10000;
        int walkCount = 0;
        DWORD64 origRSP = 0;
        
        // Quick check: are we already at managed code?
        FunctionID fid = 0;
        HRESULT hr = profilerApi->GetFunctionFromIP(reinterpret_cast<LPCBYTE>(pContext->Rip), &fid);
        if (SUCCEEDED(hr) && fid != 0) {
            OutputDebugStringA("[StackCapture] Already at managed frame (fast path)\n");
            return S_OK;
        }
        
        // Walk native frames to find managed code
        for (walkCount = 0; walkCount < MAX_WALK_EVER; ++walkCount) {
            if (pStopRequested && pStopRequested->load()) {
                OutputDebugStringA("[StackCapture] Stop requested during pre-walk\n");
                return E_ABORT;
            }
            
            // Check for stack progress
            if (origRSP != 0 && pContext->Rsp <= origRSP) {
                OutputDebugStringA("[StackCapture] Stack not progressing - terminating walk\n");
                break;
            }
            origRSP = pContext->Rsp;
            
            // Check for end of stack
            if (pContext->Rip == 0) {
                OutputDebugStringA("[StackCapture] Reached end of stack (RIP=0)\n");
                break;
            }
            
            // Log progress every 100 frames
            if (walkCount % 100 == 0 && walkCount > 0) {
                char progressLog[256];
                sprintf_s(progressLog, "[StackCapture] Unwind progress: %d frames\n", walkCount);
                OutputDebugStringA(progressLog);
            }
            
            // Try to find runtime function for current RIP
            UNWIND_HISTORY_TABLE historyTable = {};
            DWORD64 imageBase = 0;
            PRUNTIME_FUNCTION runtimeFunction = RtlLookupFunctionEntry(pContext->Rip, &imageBase, &historyTable);
            
            DWORD64 instructionPointer;
            
            if (!runtimeFunction) {
                // Leaf function - read return address from stack
                DWORD64 returnAddress = 0;
                if (!ReadReturnAddressFromStack(pContext->Rsp, &returnAddress)) {
                    char errorLog[256];
                    sprintf_s(errorLog, "[StackCapture] Failed to read return address at walk %d from RSP=0x%016llX\n",
                             walkCount, static_cast<unsigned long long>(pContext->Rsp));
                    OutputDebugStringA(errorLog);
                    return E_FAIL;
                }
                
                pContext->Rip = returnAddress;
                pContext->Rsp += sizeof(DWORD64);
                instructionPointer = returnAddress;
                
            } else {
                // Has unwind info - use function begin address (critical for CLR detection)
                instructionPointer = imageBase + runtimeFunction->BeginAddress;
                
                // Unwind to previous frame
                ULONG64 establisherFrame = 0;
                if (!SafeRtlVirtualUnwind(imageBase, pContext->Rip, runtimeFunction, pContext, &establisherFrame)) {
                    char errorLog[256];
                    sprintf_s(errorLog, "[StackCapture] RtlVirtualUnwind failed at walk %d\n", walkCount);
                    OutputDebugStringA(errorLog);
                    return E_FAIL;
                }
            }
            
            // Check if this instruction pointer is managed
            hr = profilerApi->GetFunctionFromIP(reinterpret_cast<LPCBYTE>(instructionPointer), &fid);
            if (SUCCEEDED(hr) && fid != 0) {
                char foundLog[256];
                sprintf_s(foundLog, "[StackCapture] Found managed frame at walk %d - IP=0x%016llX, FunctionID=0x%016llX\n",
                         walkCount, static_cast<unsigned long long>(instructionPointer),
                         static_cast<unsigned long long>(fid));
                OutputDebugStringA(foundLog);
                
                // Update context to point to this managed frame
                pContext->Rip = instructionPointer;
                
                char seedLog[256];
                sprintf_s(seedLog, "[StackCapture] Seed context prepared - RIP=0x%016llX, RSP=0x%016llX\n",
                         static_cast<unsigned long long>(pContext->Rip),
                         static_cast<unsigned long long>(pContext->Rsp));
                OutputDebugStringA(seedLog);
                
                return S_OK;
            }
        }
        
        // Exhausted all frames without finding managed code
        char failLog[256];
        sprintf_s(failLog, "[StackCapture] No managed frame found after %d walks - thread appears to be pure native\n", walkCount);
        OutputDebugStringA(failLog);
        
        return E_FAIL;
    }

    // InvocationQueue implementation
    InvocationQueue::InvocationQueue()
    {
        worker_ =
            std::unique_ptr<std::thread, ThreadJoinerOnDelete>(new std::thread(&InvocationQueue::WorkerLoop, this));
    }
    InvocationQueue::~InvocationQueue()
    {
        Stop();
    }
    void InvocationQueue::Stop()
    {
        bool expected = false;
        if (stop_.compare_exchange_strong(expected, true))
        {
            condVar_.notify_all();
        }
        else
        {
            stop_ = true;
            condVar_.notify_all();
        }
    }
    InvocationStatus InvocationQueue::Invoke(const std::function<void()>& fn, std::chrono::milliseconds timeout)
    {
        if (stop_.load())
            return InvocationStatus::TimedOut;
        auto item = std::make_shared<QueuedInvocation>();
        item->fn  = fn;
        auto fut  = item->completedPromise.get_future();
        {
            std::lock_guard<std::mutex> lock(mutex_);
            queue_.push_back(item);
        }
        condVar_.notify_one();
        return fut.wait_for(timeout) == std::future_status::ready ? InvocationStatus::Invoked
                                                                  : InvocationStatus::TimedOut;
    }
    void InvocationQueue::WorkerLoop()
    {
        for (;;)
        {
            std::shared_ptr<QueuedInvocation> item;
            {
                std::unique_lock<std::mutex> lock(mutex_);
                condVar_.wait(lock, [this]() { return stop_.load() || !queue_.empty(); });
                if (stop_.load())
                    break;
                if (!queue_.empty())
                {
                    item = queue_.front();
                    queue_.pop_front();
                }
                else
                    continue;
            }
            try
            {
                item->fn();
            }
            catch (...)
            {
            }
            item->completedPromise.set_value();
        }
    }

    // ProfilerApiAdapter
    HRESULT ProfilerApiAdapter::DoStackSnapshot(ThreadID              threadId,
                                                StackSnapshotCallback callback,
                                                DWORD                 infoFlags,
                                                void*                 clientData,
                                                BYTE*                 context,
                                                ULONG                 contextSize)
    {
        return profilerInfo_->DoStackSnapshot(threadId, callback, infoFlags, clientData, context, contextSize);
    }
    HRESULT ProfilerApiAdapter::GetFunctionFromIP(LPCBYTE ip, FunctionID* functionId)
    {
        return profilerInfo_->GetFunctionFromIP(ip, functionId);
    }
    HRESULT ProfilerApiAdapter::GetFunctionNameFromFunctionID(FunctionID functionId, std::string& functionName)
    {
        if (functionId == 0)
        {
            functionName.clear();
            return E_INVALIDARG;
        }
        ClassID  classId  = 0;
        ModuleID moduleId = 0;
        mdToken  token    = 0;
        HRESULT  hr       = profilerInfo_->GetFunctionInfo(functionId, &classId, &moduleId, &token);
        if (FAILED(hr))
        {
            functionName.clear();
            return hr;
        }
        CComPtr<IMetaDataImport> metaDataImport;
        hr = profilerInfo_->GetModuleMetaData(moduleId, ofRead, IID_IMetaDataImport, (IUnknown**)&metaDataImport);
        if (FAILED(hr) || !metaDataImport)
        {
            functionName.clear();
            return hr;
        }
        WCHAR methodName[512] = {};
        ULONG nameLen         = 0;
        hr = metaDataImport->GetMethodProps(token, nullptr, methodName, _countof(methodName), &nameLen, nullptr,
                                            nullptr, nullptr, nullptr, nullptr);
        if (FAILED(hr))
        {
            functionName.clear();
            return hr;
        }
        WCHAR className[512] = {};
        if (classId != 0)
        {
            mdTypeDef typeDef = 0;
            hr                = profilerInfo_->GetClassIDInfo(classId, &moduleId, &typeDef);
            if (SUCCEEDED(hr))
            {
                ULONG classNameLen = 0;
                metaDataImport->GetTypeDefProps(typeDef, className, _countof(className), &classNameLen, nullptr,
                                                nullptr);
            }
        }
        std::wstring wFuncName;
        if (className[0] != 0)
        {
            wFuncName = className;
            wFuncName += L".";
        }
        wFuncName += methodName;
        int utf8Len = WideCharToMultiByte(CP_UTF8, 0, wFuncName.c_str(), -1, nullptr, 0, nullptr, nullptr);
        if (utf8Len > 0)
        {
            functionName.resize(utf8Len - 1);
            WideCharToMultiByte(CP_UTF8, 0, wFuncName.c_str(), -1, &functionName[0], utf8Len, nullptr, nullptr);
        }
        else
        {
            functionName.clear();
            return E_FAIL;
        }
        return S_OK;
    }

    // ScopedThreadSuspend
    ScopedThreadSuspend::ScopedThreadSuspend(DWORD nativeThreadId) : threadHandle_(INVALID_HANDLE_VALUE), suspended_(false) {
        threadHandle_ = OpenThread(THREAD_GET_CONTEXT | THREAD_SUSPEND_RESUME, FALSE, nativeThreadId);
        if (threadHandle_ == NULL) {
            DWORD lastError = GetLastError();
            std::stringstream ss;
            ss << "[StackCapture] OpenThread failed for thread " << nativeThreadId 
               << ", LastError=" << FormatLastError(lastError);
            OutputDebugStringA(ss.str().c_str());
            throw std::runtime_error("Failed to open thread handle");
        }
        
        DWORD suspendCount = SuspendThread(threadHandle_);
        if (suspendCount == static_cast<DWORD>(-1)) {
            DWORD lastError = GetLastError();
            std::stringstream ss;
            ss << "[StackCapture] SuspendThread failed for thread " << nativeThreadId 
               << ", LastError=" << FormatLastError(lastError);
            OutputDebugStringA(ss.str().c_str());
            CloseHandle(threadHandle_);
            threadHandle_ = INVALID_HANDLE_VALUE;
            throw std::runtime_error("Failed to suspend thread");
        }
        
        std::stringstream ss;
        ss << "[StackCapture] Successfully suspended thread " << nativeThreadId 
           << ", previous suspend count=" << suspendCount;
        OutputDebugStringA(ss.str().c_str());
        suspended_ = true;
    }
    
    ScopedThreadSuspend::~ScopedThreadSuspend() { 
        if (threadHandle_ != INVALID_HANDLE_VALUE) { 
            if (suspended_) {
                DWORD suspendCount = ResumeThread(threadHandle_);
                if (suspendCount == static_cast<DWORD>(-1)) {
                    DWORD lastError = GetLastError();
                    std::stringstream ss;
                    ss << "[StackCapture] ResumeThread failed, LastError=" << FormatLastError(lastError);
                    OutputDebugStringA(ss.str().c_str());
                }
            }
            CloseHandle(threadHandle_); 
        } 
    }
    
    ScopedThreadSuspend::ScopedThreadSuspend(ScopedThreadSuspend&& other) noexcept
        : threadHandle_(other.threadHandle_), suspended_(other.suspended_)
    {
        other.threadHandle_ = INVALID_HANDLE_VALUE;
        other.suspended_    = false;
    }

    ScopedThreadSuspend& ScopedThreadSuspend::operator=(ScopedThreadSuspend&& other) noexcept
    {
        if (this != &other)
        {
            if (threadHandle_ != INVALID_HANDLE_VALUE)
            {
                if (suspended_)
                    ResumeThread(threadHandle_);
                CloseHandle(threadHandle_);
            }
            threadHandle_       = other.threadHandle_;
            suspended_          = other.suspended_;
            other.threadHandle_ = INVALID_HANDLE_VALUE;
            other.suspended_    = false;
        }
        return *this;
    }
    

    // StackCaptureEngine
    StackCaptureEngine::StackCaptureEngine(std::unique_ptr<IProfilerApi> profilerApi, const CaptureOptions& options)
        : profilerApi_(std::move(profilerApi)), options_(options)
    {
        invocationQueue_ = std::make_unique<InvocationQueue>();
        OutputDebugStringA("[StackCapture] Engine initialized\n");
    }
    StackCaptureEngine::~StackCaptureEngine()
    {
        Stop();
    }
    
    void StackCaptureEngine::Stop()
    {
        stopRequested_ = true;
        captureCondVar_.notify_all();
        if (invocationQueue_)
            invocationQueue_->Stop();
    }

    HRESULT StackCaptureEngine::ThreadDestroyed(ThreadID threadId) {
        std::lock_guard<std::mutex> lock(threadListMutex_);
        activeThreads_.erase(threadId);
        threadNames_.erase(threadId);
        
        // Clear canary if it was this thread
        if (canaryThread_.managedId == threadId) {
            std::stringstream ss;
            ss << "[StackCapture] Canary thread destroyed - ManagedID=" << threadId;
            OutputDebugStringA(ss.str().c_str());
            canaryThread_.reset();
        }
        
        return S_OK;
    }

    HRESULT StackCaptureEngine::ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
    {
        std::lock_guard<std::mutex> lock(threadListMutex_);
        activeThreads_[managedThreadId] = osThreadId;

        if(canaryThread_.isValid())
        {
            return S_OK;
        }

        auto nameIt      = threadNames_.find(managedThreadId);
        if (nameIt != threadNames_.end())
        {
            if (nameIt->second.find(options_.canaryThreadNamePrefix) == 0)
            {
                canaryThread_ = CanaryThreadInfo{managedThreadId, osThreadId, nameIt->second};
                std::wstringstream wss;
                wss << L"[StackCapture] Designated canary thread: ManagedID=" << managedThreadId << L", NativeID="
                    << osThreadId << L", Name=" << nameIt->second << L"\n";
                OutputDebugStringW(wss.str().c_str());
            }
        }

        // Notify waiting capture thread if canary was found
        if (canaryThread_.isValid())
        {
            captureCondVar_.notify_one();
        }

        return S_OK;
    }

    HRESULT StackCaptureEngine::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
    {
        if (!name || cchName == 0)
            return S_OK;

        std::lock_guard<std::mutex> lock(threadListMutex_);
        if (canaryThread_.isValid())
        {
            return S_OK;
        }

        std::wstring threadName(name, cchName);
        threadNames_[threadId] = threadName;

        if (threadName.find(options_.canaryThreadNamePrefix) == 0)
        {
            auto osThreadIt = activeThreads_.find(threadId);
            if (osThreadIt != activeThreads_.end())
            {
                canaryThread_ = CanaryThreadInfo{threadId, osThreadIt->second, threadName};
            }
        }

        // Notify waiting capture thread if canary was found
        if (canaryThread_.isValid())
        {
            captureCondVar_.notify_one();
        }

        return S_OK;
    }
    

    bool StackCaptureEngine::SafetyProbe() {
        if (!invocationQueue_) return true;
        
        std::stringstream logStart;
        logStart << "[StackCapture] SafetyProbe START - CanaryManagedID=" << canaryThread_.managedId
                 << ", CanaryNativeID=" << canaryThread_.nativeId;
        OutputDebugStringA(logStart.str().c_str());
        
        std::atomic<HRESULT> snapshotHr{ S_OK };
        InvocationStatus status = InvocationStatus::TimedOut;
        
        try {
            ScopedThreadSuspend canaryThread(canaryThread_.nativeId);
            CONTEXT canaryCtx = {};
            canaryCtx.ContextFlags = CONTEXT_FULL;
            
            if (!GetThreadContext(canaryThread.GetHandle(), &canaryCtx)) {
                DWORD lastError = GetLastError();
                std::stringstream ss;
                ss << "[StackCapture] GetThreadContext FAILED for canary - NativeID=" << canaryThread_.nativeId 
                   << ", LastError=" << FormatLastError(lastError);
                OutputDebugStringA(ss.str().c_str());
                return false;
            }
            
            std::stringstream ctxLog;
            ctxLog << "[StackCapture] Canary context captured - RIP=0x" << std::hex << canaryCtx.Rip 
                   << ", RSP=0x" << canaryCtx.Rsp << std::dec;
            OutputDebugStringA(ctxLog.str().c_str());
            auto canaryManagedId = canaryThread_.managedId;
            // Enqueue probe worker (uses SEH-protected helper)
            status = invocationQueue_->Invoke([this, canaryManagedId, canaryCtx, &snapshotHr]() {
                OutputDebugStringA("[StackCapture] Probe worker executing...\n");
                
                HRESULT hr = ExecuteProbeOperations(profilerApi_.get(), canaryManagedId, canaryCtx);
                snapshotHr.store(hr);
                
                std::stringstream dssLog;
                dssLog << "[StackCapture] Probe operations result=0x" << std::hex << hr << std::dec;
                OutputDebugStringA(dssLog.str().c_str());
            }, options_.probeTimeout);
            
            // Canary thread auto-resumes here via RAII
            
        } catch (const std::exception& ex) { 
            std::stringstream ss;
            ss << "[StackCapture] Exception in SafetyProbe: " << ex.what();
            OutputDebugStringA(ss.str().c_str());
            return false; 
        }
        
        if (status != InvocationStatus::Invoked) {
            std::stringstream ss;
            ss << "[StackCapture] SafetyProbe TIMED OUT - timeout=" << options_.probeTimeout.count() << "ms";
            OutputDebugStringA(ss.str().c_str());
            return false;
        }
        
        HRESULT hr = snapshotHr.load();
        if (hr == CORPROF_E_STACKSNAPSHOT_UNSAFE) {
            OutputDebugStringA("[StackCapture] DoStackSnapshot reported UNSAFE (0x80131354)\n");
            return false;
        }
        
        if (FAILED(hr)) {
            std::stringstream ss;
            ss << "[StackCapture] Probe operations failed with HRESULT=0x" << std::hex << hr << std::dec;
            OutputDebugStringA(ss.str().c_str());
            return false;
        }
        
        OutputDebugStringA("[StackCapture] SafetyProbe PASSED\n");
        return true;
    }

    HRESULT StackCaptureEngine::CaptureStackSeeded(ThreadID managedThreadId, HANDLE threadHandle, StackCaptureContext* stackCaptureContext) {
        std::stringstream startLog;
        startLog << "[StackCapture] CaptureStackSeeded START - ManagedID=" << managedThreadId;
        OutputDebugStringA(startLog.str().c_str());
        
        CONTEXT context = {}; 
        context.ContextFlags = CONTEXT_FULL;
        
        if (!GetThreadContext(threadHandle, &context)) {
            DWORD lastError = GetLastError();
            std::stringstream ss;
            ss << "[StackCapture] GetThreadContext FAILED - LastError=" << FormatLastError(lastError);
            OutputDebugStringA(ss.str().c_str());
            return E_FAIL;
        }
        
        // Prepare context (uses SEH-protected helpers internally, no C++ RAII in that path)
        HRESULT hr = PrepareContextForSnapshot(managedThreadId, threadHandle, &context, profilerApi_.get(), &stopRequested_);
        if (FAILED(hr)) {
            return hr;
        }
        stackCaptureContext->clientParams->threadId = managedThreadId;
        hr = profilerApi_->DoStackSnapshot(managedThreadId, continuous_profiler::IStackCaptureStrategy::StackSnapshotCallbackDefault, COR_PRF_SNAPSHOT_DEFAULT,
            stackCaptureContext->clientParams, reinterpret_cast<BYTE*>(&context), sizeof(CONTEXT));
        
        std::stringstream resultLog;
        resultLog << "[StackCapture] DoStackSnapshot result=0x" << std::hex << hr ;
        OutputDebugStringA(resultLog.str().c_str());
        
        return hr;
    }

    bool StackCaptureEngine::IsManagedFunction(BYTE* ip) const { FunctionID fid = 0; HRESULT hr = profilerApi_->GetFunctionFromIP(ip, &fid); return SUCCEEDED(hr) && fid != 0; }

    bool StackCaptureEngine::WaitForCanaryThread(std::chrono::milliseconds timeout) {
        std::unique_lock<std::mutex> lock(threadListMutex_);
        return captureCondVar_.wait_for(lock, timeout, [this]() {
            return stopRequested_.load() || canaryThread_.isValid();
        });
    }

    HRESULT StackCaptureEngine::CaptureStacks(std::unordered_set<ThreadID> const& threads, continuous_profiler::StackSnapshotCallbackParams* clientData)
    {
        bool canaryReady = WaitForCanaryThread();

        if (!canaryReady)
            return E_FAIL;

        for (const auto& managedId : threads)
        {
            if (stopRequested_)
                break;
            if (managedId == canaryThread_.managedId)
                continue;
            DWORD nativeId = 0;
            {
                std::lock_guard<std::mutex> lock(threadListMutex_);
                auto                        it = activeThreads_.find(managedId);
                if (it == activeThreads_.end())
                {
                    std::stringstream ss;
                    ss << "[StackCapture] Thread not found in active list: ManagedID=" << managedId;
                    OutputDebugStringA(ss.str().c_str());
                    continue;
                }
                nativeId = it->second;
            }
            try
            {
                ScopedThreadSuspend targetThread(nativeId);
                if (!SafetyProbe())
                {
                    continue;
                }
                clientData->threadId = managedId;
                StackCaptureContext stackCaptureContext{0, &stopRequested_, clientData};
                HRESULT             hr = CaptureStackSeeded(managedId, targetThread.GetHandle(), &stackCaptureContext);
                CaptureResult       result = SUCCEEDED(hr) ? CaptureResult::Success : CaptureResult::Failed;
            }
            catch (const std::exception& ex)
            {
                std::stringstream ss;
                ss << "[StackCapture] Exception capturing thread " << managedId << ": " << ex.what();
                OutputDebugStringA(ss.str().c_str());
                std::vector<StackFrame> empty;
            }
        }
        return S_OK;
    }

} // namespace ProfilerStackCapture
#endif 