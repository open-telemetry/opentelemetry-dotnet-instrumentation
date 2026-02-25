/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "environment_variables_util.h"
#include "standalone_deployment_detection.h"

namespace trace
{

namespace
{
// Resolves the assembly redirection env variable, falling back to the
// legacy .NET Framework variable on Windows when the primary is unset.
WSTRING GetAssemblyRedirectionRawValue()
{
    auto value = GetEnvironmentValue(environment::assembly_redirection_enabled);

#ifdef _WIN32
    // For .NET Framework, if the primary variable is NOT set (neither True nor False),
    // check the legacy fallback variable.
    if (!TrueCondition(value) && !FalseCondition(value))
    {
        value = GetEnvironmentValue(environment::assembly_redirection_enabled_netfx_legacy);
    }
#endif

    return value;
}
} // namespace

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
    // Default is true for standalone deployment (needs runtime redirection)
    // and false for non-standalone deployments (e.g., NuGet-based, where build-time resolution handles it).
    ToBooleanWithDefault(GetAssemblyRedirectionRawValue(), IsStandaloneDeployment());
}

} // namespace trace