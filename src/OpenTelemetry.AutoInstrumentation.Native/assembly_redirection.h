/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "assembly_redirection_netfx.h"
#include "assembly_redirection_net.h"
#include "cor_profiler.h"

#define STR(Z1) #Z1
#define AUTO_MAJOR STR(OTEL_AUTO_VERSION_MAJOR) 

namespace trace
{
void CorProfiler::InitAssemblyRedirectsMap()
{
    const USHORT auto_major = atoi(AUTO_MAJOR);

    assembly_version_redirect_map_.insert({
        ASSEMBLY_REDIRECTION_NETFX,
        ASSEMBLY_REDIRECTION_NET
    });
}
}
