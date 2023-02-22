#include "environment_variables_util.h"

namespace trace {

bool DisableOptimizations() {
  CheckIfTrue(GetEnvironmentValue(environment::clr_disable_optimizations));
}

bool EnableInlining() {
  ToBooleanWithDefault(GetEnvironmentValue(environment::clr_enable_inlining), true);
}

bool IsNGENEnabled() {
  ToBooleanWithDefault(GetEnvironmentValue(environment::clr_enable_ngen),
                       false);
}

bool IsDumpILRewriteEnabled() {
  CheckIfTrue(GetEnvironmentValue(environment::dump_il_rewrite_enabled));
}

bool IsAzureAppServices() {
  CheckIfTrue(GetEnvironmentValue(environment::azure_app_services));
}

bool AreTracesEnabled() {
  ToBooleanWithDefault(GetEnvironmentValue(environment::traces_enabled), true);
}

bool AreMetricsEnabled() {
  ToBooleanWithDefault(GetEnvironmentValue(environment::metrics_enabled), true);
}

bool AreLogsEnabled() {
  ToBooleanWithDefault(GetEnvironmentValue(environment::logs_enabled), true);
}

bool IsNetFxAssemblyRedirectionEnabled() {
  ToBooleanWithDefault(GetEnvironmentValue(environment::netfx_assembly_redirection_enabled), true);
}

bool AreInstrumentationsEnabledByDefault() {
  ToBooleanWithDefault(GetEnvironmentValue(environment::instrumentation_enabled), true);
}

bool AreTracesInstrumentationsEnabledByDefault(const bool enabled_if_not_configured) {
  ToBooleanWithDefault(GetEnvironmentValue(environment::traces_instrumentation_enabled), enabled_if_not_configured);
}

bool AreMetricsInstrumentationsEnabledByDefault(const bool enabled_if_not_configured) {
  ToBooleanWithDefault(GetEnvironmentValue(environment::metrics_instrumentation_enabled), enabled_if_not_configured);
}

bool AreLogsInstrumentationsEnabledByDefault(const bool enabled_if_not_configured) {
  ToBooleanWithDefault(GetEnvironmentValue(environment::logs_instrumentation_enabled), enabled_if_not_configured);
}

}  // namespace trace