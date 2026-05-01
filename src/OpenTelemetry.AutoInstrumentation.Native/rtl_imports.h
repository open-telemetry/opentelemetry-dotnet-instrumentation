// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_RTL_IMPORTS_H_
#define OTEL_RTL_IMPORTS_H_

#if defined(_WIN32) && defined(_M_AMD64)
#include <windows.h>
#ifndef DECLSPEC_IMPORT
#define DECLSPEC_IMPORT __declspec(dllimport)
#endif

extern "C"
{
    DECLSPEC_IMPORT PRUNTIME_FUNCTION NTAPI RtlLookupFunctionEntry(DWORD64               ControlPc,
                                                                   PDWORD64              ImageBase,
                                                                   PUNWIND_HISTORY_TABLE HistoryTable);

    DECLSPEC_IMPORT PEXCEPTION_ROUTINE NTAPI RtlVirtualUnwind(DWORD                          HandlerType,
                                                              DWORD64                        ImageBase,
                                                              DWORD64                        ControlPc,
                                                              PRUNTIME_FUNCTION              FunctionEntry,
                                                              PCONTEXT                       ContextRecord,
                                                              PVOID*                         HandlerData,
                                                              PDWORD64                       EstablisherFrame,
                                                              PKNONVOLATILE_CONTEXT_POINTERS ContextPointers);
}

#endif // defined(_WIN32) && defined(_M_AMD64)
#endif // OTEL_RTL_IMPORTS_H_