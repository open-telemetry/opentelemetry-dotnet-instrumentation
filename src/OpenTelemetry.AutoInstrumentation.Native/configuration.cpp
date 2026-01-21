/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "configuration.h"

namespace trace
{

bool sqlclient_netfx_ilrewrite_enabled = false;

bool netfx_assembly_redirection_enabled = true;

bool IsSqlClientNetFxILRewriteEnabled()
{
    return sqlclient_netfx_ilrewrite_enabled;
}

void SetSqlClientNetFxILRewriteEnabled(bool enabled)
{
    sqlclient_netfx_ilrewrite_enabled = enabled;
}

bool IsNetFxAssemblyRedirectionEnabled()
{
    return netfx_assembly_redirection_enabled;
}
void SetNetFxAssemblyRedirectionEnabled(bool enabled)
{
    netfx_assembly_redirection_enabled = enabled;
}

} // namespace trace