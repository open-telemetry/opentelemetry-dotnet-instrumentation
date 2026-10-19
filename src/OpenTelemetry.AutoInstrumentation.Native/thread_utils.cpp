// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "thread_utils.h"

#ifdef __linux__
#include <sys/prctl.h>
#elif __APPLE__
#include <pthread.h>
#endif

#ifdef _WIN32

std::once_flag                          Threads::s_threadDescriptionInit;
Threads::SetThreadDescriptionDelegate_t Threads::s_setThreadDescriptionDelegate = nullptr;

void Threads::InitDelegates_GetSetThreadDescription()
{
    HMODULE moduleHandle = GetModuleHandle(WStr("KernelBase.dll"));

    if (NULL == moduleHandle)
    {
        moduleHandle = LoadLibrary(WStr("KernelBase.dll"));
    }

    if (NULL != moduleHandle)
    {
        s_setThreadDescriptionDelegate =
            reinterpret_cast<SetThreadDescriptionDelegate_t>(GetProcAddress(moduleHandle, "SetThreadDescription"));
    }
}

Threads::SetThreadDescriptionDelegate_t Threads::GetDelegate_SetThreadDescription()
{
    std::call_once(s_threadDescriptionInit, InitDelegates_GetSetThreadDescription);
    return s_setThreadDescriptionDelegate;
}

#endif // #ifdef _WINDOWS

bool Threads::SetNativeThreadName(const WCHAR* description)
{
#ifdef _WIN32
    // The SetThreadDescription(..) API is only available on recent Windows versions and must be called dynamically.
    // We attempt to link to it at runtime, and if we do not succeed, this operation is a No-Op.
    // https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setthreaddescription#remarks

    SetThreadDescriptionDelegate_t setThreadDescriptionDelegate = GetDelegate_SetThreadDescription();
    if (nullptr == setThreadDescriptionDelegate)
    {
        return false;
    }

    HRESULT hr = setThreadDescriptionDelegate(GetCurrentThread(), description);
    return SUCCEEDED(hr);
#elif __linux__
    const auto name = trace::ToString(description);
    return prctl(PR_SET_NAME, name.data(), 0, 0, 0) == 0;
#else
    const auto name = trace::ToString(description);
    return pthread_setname_np(name.data()) == 0;
#endif
}
