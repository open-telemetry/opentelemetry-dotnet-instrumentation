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

std::optional<bool> IsAssemblyRedirectionEnabled()
{
    // 1. Get the primary assembly redirection variable
    auto assemblyRedirection = []() -> std::optional<bool>
    { ToBooleanWithDefault(GetEnvironmentValue(environment::assembly_redirection_enabled), std::nullopt); }();

#ifdef _WIN32
    // 2. For .NET Framework, fallback to legacy variable if primary is not set
    if (!assemblyRedirection)
    {
        assemblyRedirection = []() -> std::optional<bool> {
            ToBooleanWithDefault(GetEnvironmentValue(environment::assembly_redirection_enabled_netfx_legacy),
                                 std::nullopt);
        }();
    }
#endif

    return assemblyRedirection;
}

} // namespace trace