/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_DEPLOYMENT_DETECTION_H_
#define OTEL_CLR_PROFILER_DEPLOYMENT_DETECTION_H_

#include "logger.h"
#include "managed_profiler_location_helper.h"

namespace trace
{

// Filename of the OpenTelemetry managed profiler assembly used as the anchor for deployment detection.
const WSTRING otel_instrumentation_assembly_filename = WStr("OpenTelemetry.AutoInstrumentation.dll");

inline bool IsStandaloneDeployment(const WSTRING& profiler_path, const WSTRING& tracer_home)
{
    const auto found = FindManagedAssembly(otel_instrumentation_assembly_filename, profiler_path, tracer_home);
    if (found.empty())
    {
        // The managed profiler assembly was not found at any expected location.
        // We cannot determine deployment type with certainty, so conservatively assume
        // it's non-standalone. This prevents native assembly redirection to the dependencies
        // that are not actually bundled.
        Logger::Info("IsStandaloneDeployment: ", otel_instrumentation_assembly_filename,
                     " not found at any expected location, treating as non-standalone.");
        return false;
    }

    if (found.is_standalone)
    {
        Logger::Info("IsStandaloneDeployment: Detected standalone deployment: ", otel_instrumentation_assembly_filename,
                     " found at '", ToString(PATH_TO_WSTRING(found.path)), "'.");
    }
    else
    {
        Logger::Info("IsStandaloneDeployment: Detected non-standalone (e.g., NuGet-based) deployment: ",
                     otel_instrumentation_assembly_filename, " found at '", ToString(PATH_TO_WSTRING(found.path)),
                     "'.");
    }

    return found.is_standalone;
}

} // namespace trace

#endif // OTEL_CLR_PROFILER_DEPLOYMENT_DETECTION_H_
