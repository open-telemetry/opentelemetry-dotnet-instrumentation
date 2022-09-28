#include "rejit_handler.h"

#include "logger.h"

namespace trace
{

//
// RejitHandlerModuleMethod
//

RejitHandlerModuleMethod::RejitHandlerModuleMethod(mdMethodDef methodDef, RejitHandlerModule* module)
{
    m_methodDef = methodDef;
    m_pFunctionControl = nullptr;
    m_module = module;
    m_functionInfo = nullptr;
    m_methodReplacement = nullptr;
}

mdMethodDef RejitHandlerModuleMethod::GetMethodDef()
{
    return m_methodDef;
}

RejitHandlerModule* RejitHandlerModuleMethod::GetModule()
{
    return m_module;
}

ICorProfilerFunctionControl* RejitHandlerModuleMethod::GetFunctionControl()
{
    return m_pFunctionControl;
}

void RejitHandlerModuleMethod::SetFunctionControl(ICorProfilerFunctionControl* pFunctionControl)
{
    m_pFunctionControl = pFunctionControl;
}

FunctionInfo* RejitHandlerModuleMethod::GetFunctionInfo()
{
    return m_functionInfo.get();
}

void RejitHandlerModuleMethod::SetFunctionInfo(const FunctionInfo& functionInfo)
{
    m_functionInfo = std::make_unique<FunctionInfo>(functionInfo);
}

MethodReplacement* RejitHandlerModuleMethod::GetMethodReplacement()
{
    return m_methodReplacement.get();
}

void RejitHandlerModuleMethod::SetMethodReplacement(const MethodReplacement& methodReplacement)
{
    m_methodReplacement = std::make_unique<MethodReplacement>(methodReplacement);
}

void RejitHandlerModuleMethod::RequestRejitForInlinersInModule(ModuleID moduleId)
{
    Logger::Debug("RejitHandlerModuleMethod::RequestRejitForInlinersInModule: ", moduleId);
    std::lock_guard<std::mutex> guard(m_ngenModulesLock);

    // We check first if we already processed this module to skip it.
    auto find_res = m_ngenModules.find(moduleId);
    if (find_res != m_ngenModules.end())
    {
        return;
    }

    // Enumerate all inliners and request rejit
    ModuleID currentModuleId = m_module->GetModuleId();
    mdMethodDef currentMethodDef = m_methodDef;
    RejitHandler* handler = m_module->GetHandler();
    ICorProfilerInfo6* pInfo = handler->GetCorProfilerInfo6();

    if (pInfo != nullptr)
    {
        // Now we enumerate all methods that inline the current methodDef
        BOOL incompleteData = false;
        ICorProfilerMethodEnum* methodEnum;

        HRESULT hr = pInfo->EnumNgenModuleMethodsInliningThisMethod(moduleId, currentModuleId, currentMethodDef,
                                                                    &incompleteData, &methodEnum);
        if (SUCCEEDED(hr))
        {
            COR_PRF_METHOD method;
            unsigned int total = 0;
            std::vector<ModuleID> modules;
            std::vector<mdMethodDef> methods;
            while (methodEnum->Next(1, &method, NULL) == S_OK)
            {
                Logger::Debug("NGEN:: Asking rewrite for inliner [ModuleId=", method.moduleId,
                              ",MethodDef=", method.methodId, "]");
                modules.push_back(method.moduleId);
                methods.push_back(method.methodId);
                total++;
            }
            methodEnum->Release();
            methodEnum = nullptr;
            if (total > 0)
            {
                handler->RequestRejit(modules, methods);
                Logger::Info("NGEN:: Processed with ", total, " inliners [ModuleId=", currentModuleId,
                             ",MethodDef=", currentMethodDef, "]");
            }

            if (!incompleteData)
            {
                m_ngenModules[moduleId] = true;
            }
            else
            {
                Logger::Warn("NGen inliner data for module '", moduleId, "' is incomplete.");
            }
        }
        else if (hr == E_INVALIDARG)
        {
            Logger::Info("NGEN:: Error Invalid arguments in [ModuleId=", currentModuleId,
                         ",MethodDef=", currentMethodDef, ", HR=", hr, "]");
        }
        else if (hr == CORPROF_E_DATAINCOMPLETE)
        {
            Logger::Info("NGEN:: Error Incomplete data in [ModuleId=", currentModuleId, ",MethodDef=", currentMethodDef, ", HR=", hr, "]");
        }
        else
        {
            Logger::Info("NGEN:: Error in [ModuleId=", currentModuleId, ",MethodDef=", currentMethodDef, ", HR=", hr, "]");
        }
    }
}

//
// RejitHandlerModule
//

RejitHandlerModule::RejitHandlerModule(ModuleID moduleId, RejitHandler* handler)
{
    m_moduleId = moduleId;
    m_metadata = nullptr;
    m_handler = handler;
}

ModuleID RejitHandlerModule::GetModuleId()
{
    return m_moduleId;
}

RejitHandler* RejitHandlerModule::GetHandler()
{
    return m_handler;
}

ModuleMetadata* RejitHandlerModule::GetModuleMetadata()
{
    return m_metadata;
}

void RejitHandlerModule::SetModuleMetadata(ModuleMetadata* metadata)
{
    m_metadata = metadata;
}

RejitHandlerModuleMethod* RejitHandlerModule::GetOrAddMethod(mdMethodDef methodDef)
{
    std::lock_guard<std::mutex> guard(m_methods_lock);

    auto find_res = m_methods.find(methodDef);
    if (find_res != m_methods.end())
    {
        return find_res->second.get();
    }

    RejitHandlerModuleMethod* methodHandler = new RejitHandlerModuleMethod(methodDef, this);
    m_methods[methodDef] = std::unique_ptr<RejitHandlerModuleMethod>(methodHandler);
    return methodHandler;
}

bool RejitHandlerModule::TryGetMethod(mdMethodDef methodDef, RejitHandlerModuleMethod** methodHandler)
{
    std::lock_guard<std::mutex> guard(m_methods_lock);

    auto find_res = m_methods.find(methodDef);
    if (find_res != m_methods.end())
    {
        *methodHandler = find_res->second.get();
        return true;
    }
    *methodHandler = nullptr;
    return false;
}

bool RejitHandlerModule::ContainsMethod(mdMethodDef methodDef)
{
    std::lock_guard<std::mutex> guard(m_methods_lock);
    return m_methods.find(methodDef) != m_methods.end();
}

void RejitHandlerModule::RequestRejitForInlinersInModule(ModuleID moduleId)
{
    std::lock_guard<std::mutex> guard(m_methods_lock);
    for (const auto& method : m_methods)
    {
        method.second.get()->RequestRejitForInlinersInModule(moduleId);
    }
}

void RejitHandler::RequestRejitForInlinersInModule(ModuleID moduleId)
{
    std::lock_guard<std::mutex> guard(m_modules_lock);
    for (const auto& mod : m_modules)
    {
        mod.second->RequestRejitForInlinersInModule(moduleId);
    }
}

RejitHandler::RejitHandler(ICorProfilerInfo6* pInfo,
                           std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> rewriteCallback)
{
    m_profilerInfo6 = pInfo;
    m_rewriteCallback = rewriteCallback;
}

RejitHandlerModule* RejitHandler::GetOrAddModule(ModuleID moduleId)
{
    std::lock_guard<std::mutex> guard(m_modules_lock);

    auto find_res = m_modules.find(moduleId);
    if (find_res != m_modules.end())
    {
        return find_res->second.get();
    }

    RejitHandlerModule* moduleHandler = new RejitHandlerModule(moduleId, this);
    m_modules[moduleId] = std::unique_ptr<RejitHandlerModule>(moduleHandler);
    return moduleHandler;
}

bool RejitHandler::TryGetModule(ModuleID moduleId, RejitHandlerModule** moduleHandler)
{
    std::lock_guard<std::mutex> guard(m_modules_lock);

    auto find_res = m_modules.find(moduleId);
    if (find_res != m_modules.end())
    {
        *moduleHandler = find_res->second.get();
        return true;
    }
    *moduleHandler = nullptr;
    return false;
}

void RejitHandler::RemoveModule(ModuleID moduleId)
{
    std::lock_guard<std::mutex> guard(m_modules_lock);
    m_modules.erase(moduleId);
}

void RejitHandler::AddNGenModule(ModuleID moduleId)
{
    std::lock_guard<std::mutex> guard(m_ngenModules_lock);
    m_ngenModules.push_back(moduleId);
    RequestRejitForInlinersInModule(moduleId);
}

void RejitHandler::RequestRejit(std::vector<ModuleID>& modulesVector, std::vector<mdMethodDef>& modulesMethodDef)
{
    const size_t length = modulesMethodDef.size();

    // Create module and methods metadata.
    for (size_t i = 0; i < length; i++)
    {
        GetOrAddModule(modulesVector[i])->GetOrAddMethod(modulesMethodDef[i]);
    }

    // Even if ICorProfilerInfo10, or later, is available the code leverages the fact
    // that this is a startup profiler so there is no need to handle an attach scenario.
    // Instead of using RequestReJITWithInliners to handle inlined methods that are targeted
    // for instrumentation the code uses the ICorProfilerCallback::JITInlining callback instead.
    // On the callback the profiler blocks the inlining of any method targeted for instrumentation.
    HRESULT hr = m_profilerInfo6->RequestReJIT((ULONG) length, modulesVector.data(), modulesMethodDef.data());
    if (SUCCEEDED(hr))
    {
        Logger::Info("Request ReJIT done for ", length, " methods");
    }
    else
    {
        Logger::Warn("Error requesting ReJIT for ", length, " methods");
    }
}

void RejitHandler::Shutdown()
{
    m_modules.clear();
    m_profilerInfo6 = nullptr;
    m_rewriteCallback = nullptr;
}

HRESULT RejitHandler::NotifyReJITParameters(ModuleID moduleId, mdMethodDef methodId,
                                            ICorProfilerFunctionControl* pFunctionControl, ModuleMetadata* metadata)
{
    auto moduleHandler = GetOrAddModule(moduleId);
    moduleHandler->SetModuleMetadata(metadata);
    auto methodHandler = moduleHandler->GetOrAddMethod(methodId);
    methodHandler->SetFunctionControl(pFunctionControl);

    if (methodHandler->GetMethodDef() == mdMethodDefNil)
    {
        Logger::Warn("NotifyReJITCompilationStarted: mdMethodDef is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (methodHandler->GetFunctionControl() == nullptr)
    {
        Logger::Warn("NotifyReJITCompilationStarted: ICorProfilerFunctionControl is missing "
                     "for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (methodHandler->GetFunctionInfo() == nullptr)
    {
        Logger::Warn("NotifyReJITCompilationStarted: FunctionInfo is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (methodHandler->GetMethodReplacement() == nullptr)
    {
        Logger::Warn("NotifyReJITCompilationStarted: MethodReplacement is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (moduleHandler->GetModuleId() == 0)
    {
        Logger::Warn("NotifyReJITCompilationStarted: ModuleID is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (moduleHandler->GetModuleMetadata() == nullptr)
    {
        Logger::Warn("NotifyReJITCompilationStarted: ModuleMetadata is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    return m_rewriteCallback(moduleHandler, methodHandler);
}

HRESULT RejitHandler::NotifyReJITCompilationStarted(FunctionID functionId, ReJITID rejitId)
{
    return S_OK;
}

ICorProfilerInfo6* RejitHandler::GetCorProfilerInfo6()
{
    return m_profilerInfo6;
}

void RejitHandler::RequestRejitForNGenInliners()
{
    if (m_profilerInfo6 != nullptr)
    {
        std::lock_guard<std::mutex> guard(m_ngenModules_lock);
        for (const auto& mod : m_ngenModules)
        {
            RequestRejitForInlinersInModule(mod);
        }
    }
}

} // namespace trace