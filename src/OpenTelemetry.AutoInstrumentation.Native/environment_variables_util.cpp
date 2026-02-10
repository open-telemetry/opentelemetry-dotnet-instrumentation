/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "environment_variables_util.h"

namespace trace
{

bool DisableOptimizations()
{
    CheckIfTrue(GetEnvironmentValue(environment::clr_disable_optimizations));
}

bool EnableInlining()
{
    ToBooleanWithDefault(GetEnvironmentValue(environment::clr_enable_inlining), true);
}

bool IsNGENEnabled()
{
    ToBooleanWithDefault(GetEnvironmentValue(environment::clr_enable_ngen), false);
}

bool IsDumpILRewriteEnabled()
{
    CheckIfTrue(GetEnvironmentValue(environment::dump_il_rewrite_enabled));
}

bool IsAzureAppServices()
{
    CheckIfTrue(GetEnvironmentValue(environment::azure_app_services));
}

bool IsFailFastEnabled()
{
    CheckIfTrue(GetEnvironmentValue(environment::fail_fast_enabled));
}

bool IsAssemblyRedirectionEnabled()
{
    auto assemblyRedirectEnvValue = GetEnvironmentValue(environment::assembly_redirection_enabled);

#ifdef _WIN32
    // For .Net Framework if the primary variable is NOT set (neither True nor False),
    // then we consider it "unset" and check the legacy fallback.
    if (!TrueCondition(assemblyRedirectEnvValue) && !FalseCondition(assemblyRedirectEnvValue))
    {
        assemblyRedirectEnvValue = GetEnvironmentValue(environment::assembly_redirection_enabled_netfx_legacy);
    }
#endif

    ToBooleanWithDefault(assemblyRedirectEnvValue, true);
}

} // namespace trace