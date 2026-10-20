// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma once
#include <mutex>

#ifdef _WIN32
#include <windows.h>
#endif

#include "string_utils.h" // NOLINT

class Threads
{
public:
    static bool SetNativeThreadName(const WCHAR* description);

private:
#ifdef _WIN32
    typedef HRESULT(__stdcall* SetThreadDescriptionDelegate_t)(HANDLE threadHandle, PCWSTR pThreadDescription);

    static std::once_flag s_threadDescriptionInit;
    static SetThreadDescriptionDelegate_t s_setThreadDescriptionDelegate;

    static void InitDelegates_GetSetThreadDescription();
    static SetThreadDescriptionDelegate_t GetDelegate_SetThreadDescription();
#endif
};