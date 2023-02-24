/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_REJIT_HANDLER_H_
#define OTEL_CLR_PROFILER_REJIT_HANDLER_H_

#include <atomic>
#include <mutex>
#include <string>
#include <unordered_map>
#include <vector>

#include "cor.h"
#include "corprof.h"
#include "module_metadata.h"

namespace trace
{

// forward declarations...
class RejitHandlerModule;
class RejitHandler;

/// <summary>
/// Rejit handler representation of a method
/// </summary>
class RejitHandlerModuleMethod
{
private:
    mdMethodDef m_methodDef;
    ICorProfilerFunctionControl* m_pFunctionControl;
    std::unique_ptr<FunctionInfo> m_functionInfo;
    std::unique_ptr<MethodReplacement> m_methodReplacement;

    std::mutex m_ngenModulesLock;
    std::unordered_map<ModuleID, bool> m_ngenModules;

    RejitHandlerModule* m_module;

public:
    RejitHandlerModuleMethod(mdMethodDef methodDef, RejitHandlerModule* module);
    mdMethodDef GetMethodDef();
    RejitHandlerModule* GetModule();

    ICorProfilerFunctionControl* GetFunctionControl();
    void SetFunctionControl(ICorProfilerFunctionControl* pFunctionControl);

    FunctionInfo* GetFunctionInfo();
    void SetFunctionInfo(const FunctionInfo& functionInfo);

    MethodReplacement* GetMethodReplacement();
    void SetMethodReplacement(const MethodReplacement& methodReplacement);

    void RequestRejitForInlinersInModule(ModuleID moduleId);
};

/// <summary>
/// Rejit handler representation of a module
/// </summary>
class RejitHandlerModule
{
private:
    ModuleID m_moduleId;
    ModuleMetadata* m_metadata;
    std::mutex m_methods_lock;
    std::unordered_map<mdMethodDef, std::unique_ptr<RejitHandlerModuleMethod>> m_methods;
    RejitHandler* m_handler;

public:
    RejitHandlerModule(ModuleID moduleId, RejitHandler* handler);
    ModuleID GetModuleId();
    RejitHandler* GetHandler();

    ModuleMetadata* GetModuleMetadata();
    void SetModuleMetadata(ModuleMetadata* metadata);

    RejitHandlerModuleMethod* GetOrAddMethod(mdMethodDef methodDef);
    bool TryGetMethod(mdMethodDef methodDef, RejitHandlerModuleMethod** methodHandler);
    bool ContainsMethod(mdMethodDef methodDef);

    void RequestRejitForInlinersInModule(ModuleID moduleId);
};

/// <summary>
/// Class to control the ReJIT mechanism and to make sure all the required
/// information is present before calling a method rewrite
/// </summary>
class RejitHandler
{
private:
    std::mutex m_modules_lock;
    std::unordered_map<ModuleID, std::unique_ptr<RejitHandlerModule>> m_modules;

    ICorProfilerInfo7* m_profilerInfo7;

    std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> m_rewriteCallback;

    std::mutex m_ngenModules_lock;
    std::vector<ModuleID> m_ngenModules;

    void RequestRejitForInlinersInModule(ModuleID moduleId);

public:
    RejitHandler(ICorProfilerInfo7* pInfo,
                 std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> rewriteCallback);

    RejitHandlerModule* GetOrAddModule(ModuleID moduleId);

    bool TryGetModule(ModuleID moduleId, RejitHandlerModule** moduleHandler);
    void RemoveModule(ModuleID moduleId);

    void AddNGenModule(ModuleID moduleId);

    void RequestRejit(std::vector<ModuleID>& modulesVector, std::vector<mdMethodDef>& modulesMethodDef);

    void Shutdown();

    HRESULT NotifyReJITParameters(ModuleID moduleId, mdMethodDef methodId,
                                  ICorProfilerFunctionControl* pFunctionControl, ModuleMetadata* metadata);
    HRESULT NotifyReJITCompilationStarted(FunctionID functionId, ReJITID rejitId);

    ICorProfilerInfo7* GetCorProfilerInfo7();

    void RequestRejitForNGenInliners();
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_REJIT_HANDLER_H_