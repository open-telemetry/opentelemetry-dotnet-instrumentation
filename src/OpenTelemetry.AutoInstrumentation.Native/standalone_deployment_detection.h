/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_STANDALONE_DEPLOYMENT_DETECTION_H_
#define OTEL_CLR_PROFILER_STANDALONE_DEPLOYMENT_DETECTION_H_

namespace trace
{

// Detects whether the current deployment is standalone (e.g., zip/installer-based)
// by checking the folder structure under OTEL_DOTNET_AUTO_HOME.
// Standalone deployment has net/ and netfx/ subdirectories;
// non-standalone deployments (e.g., NuGet-based) do not.
bool IsStandaloneDeployment();

} // namespace trace

#endif // OTEL_CLR_PROFILER_STANDALONE_DEPLOYMENT_DETECTION_H_
