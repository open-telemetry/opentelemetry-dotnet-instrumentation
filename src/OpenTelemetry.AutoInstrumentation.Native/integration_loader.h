#ifndef OTEL_CLR_PROFILER_INTEGRATION_LOADER_H_
#define OTEL_CLR_PROFILER_INTEGRATION_LOADER_H_

#include <fstream>
#include <locale>
#include <nlohmann/json.hpp>
#include <string>
#include <vector>

#include "integration.h"
#include "macros.h"

namespace trace
{

using json = nlohmann::json;

class LoadIntegrationConfiguration {
public:
  LoadIntegrationConfiguration(bool traces_enabled,
                               std::vector<WSTRING> enabled_trace_integration_names,
                               std::vector<WSTRING> disabled_trace_integration_names,
                               bool logs_enabled,
                               std::vector<WSTRING> enabled_log_integration_names,
                               std::vector<WSTRING> disabled_log_integration_names)
    : traces_enabled(traces_enabled),
      enabledTraceIntegrationNames(std::move(enabled_trace_integration_names)),
      disabledTraceIntegrationNames(std::move(disabled_trace_integration_names)),
      logs_enabled(logs_enabled),
      enabledLogIntegrationNames(std::move(enabled_log_integration_names)),
      disabledLogIntegrationNames(std::move(disabled_log_integration_names)) {
  }

  const bool traces_enabled;
  const std::vector<WSTRING> enabledTraceIntegrationNames;
  const std::vector<WSTRING> disabledTraceIntegrationNames;
  const bool logs_enabled;
  const std::vector<WSTRING> enabledLogIntegrationNames;
  const std::vector<WSTRING> disabledLogIntegrationNames;
};

// LoadIntegrationsFromEnvironment loads integrations from any files specified
// in the OTEL_DOTNET_AUTO_INTEGRATIONS_FILE environment variable
void LoadIntegrationsFromEnvironment(
    std::vector<IntegrationMethod>& integrationMethods,
    const LoadIntegrationConfiguration& configuration);

// LoadIntegrationsFromFile loads the integrations from a file
void LoadIntegrationsFromFile(
    const WSTRING& file_path,
    std::vector<IntegrationMethod>& integrationMethods,
    const LoadIntegrationConfiguration& configuration);

// LoadIntegrationsFromFile loads the integrations from a stream
void LoadIntegrationsFromStream(
    std::istream& stream,
    std::vector<IntegrationMethod>& integrationMethods,
    const LoadIntegrationConfiguration& configuration);

namespace
{
    void IntegrationFromJson(const json::value_type& src,
                         std::vector<IntegrationMethod>& integrationMethods,
                         const LoadIntegrationConfiguration& configuration);

    void MethodReplacementFromJson(const json::value_type& src, const WSTRING& integrationName,
                                   std::vector<IntegrationMethod>& integrationMethods);

    MethodReference MethodReferenceFromJson(const json::value_type& src, const bool is_target_method,
                                            const bool is_wrapper_method);

} // namespace

} // namespace trace

#endif
