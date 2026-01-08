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

bool IsNetFxAssemblyRedirectionEnabled()
{
    ToBooleanWithDefault(GetEnvironmentValue(environment::netfx_assembly_redirection_enabled), true);
}

} // namespace trace