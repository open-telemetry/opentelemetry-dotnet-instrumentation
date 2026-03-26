/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef ASSEMBLY_REDIRECTION_H
#define ASSEMBLY_REDIRECTION_H

#include "assembly_redirection_netfx.h"
#include "assembly_redirection_net.h"
#include "cor_profiler.h"

#define STR(Z1) #Z1
#define AUTO_MAJOR STR(OTEL_AUTO_VERSION_MAJOR) 

// Macro to handle cross-platform UTF-16 string literals
#ifdef _WIN32
#define _W(s) L##s
#else
#define _W(s) u##s
#endif

namespace trace
{
inline void CorProfiler::InitAssemblyRedirectsMap()
{
    const USHORT auto_major = atoi(AUTO_MAJOR);

    assembly_version_redirect_map_.insert({
        ASSEMBLY_REDIRECTION_NETFX,
        ASSEMBLY_REDIRECTION_NET
    });
}
} // namespace trace

#endif // ASSEMBLY_REDIRECTION_H
