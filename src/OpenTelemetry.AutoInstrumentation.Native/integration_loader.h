/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_INTEGRATION_LOADER_H_
#define OTEL_CLR_PROFILER_INTEGRATION_LOADER_H_

#include <string>
#include <vector>

#include "string.h"
// #include "integration.h"
// #include "macros.h"

namespace trace
{

class LoadIntegrationConfiguration {
public:
  LoadIntegrationConfiguration(bool traces_enabled,
                               std::vector<WSTRING> enabled_trace_integration_names,
                               bool metrics_enabled,
                               std::vector<WSTRING> enabled_metric_integration_names,
                               bool logs_enabled,
                               std::vector<WSTRING> enabled_log_integration_names)
    : traces_enabled(traces_enabled),
      enabledTraceIntegrationNames(std::move(enabled_trace_integration_names)),
      metrics_enabled(metrics_enabled),
      enabledMetricIntegrationNames(std::move(enabled_metric_integration_names)),
      logs_enabled(logs_enabled),
      enabledLogIntegrationNames(std::move(enabled_log_integration_names)) {
  }

  const bool traces_enabled;
  const std::vector<WSTRING> enabledTraceIntegrationNames;
  const bool metrics_enabled;
  const std::vector<WSTRING> enabledMetricIntegrationNames;
  const bool logs_enabled;
  const std::vector<WSTRING> enabledLogIntegrationNames;
};

} // namespace trace

#endif