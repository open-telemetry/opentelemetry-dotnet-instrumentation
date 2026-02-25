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

// Filename of the OpenTelemetry managed profiler assembly. Used as the anchor to detect
// deployment type: if this assembly is found under net/ or netfx/ subdirectories, the
// deployment is standalone; otherwise it's non-standalone (e.g., NuGet-based).
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

    // In a standalone deployment the managed profiler lives inside a net/ or netfx/ subdirectory.
    // In a NuGet deployment it sits directly alongside the native profiler
    // or at the tracer_home root, so its immediate parent is not one of those subdirectories.
    const auto  parent_dir_name = found.parent_path().filename();
    const bool  isStandalone    = parent_dir_name == standalone_net_subdir
#ifdef _WIN32
        || parent_dir_name == standalone_netfx_subdir
#endif
        ;

    if (isStandalone)
    {
        Logger::Info("IsStandaloneDeployment: Detected standalone deployment: ",
                     otel_instrumentation_assembly_filename, " found at '", ToString(PATH_TO_WSTRING(found)), "'.");
    }
    else
    {
        Logger::Info("IsStandaloneDeployment: Detected non-standalone (e.g., NuGet-based) deployment: ",
                     otel_instrumentation_assembly_filename, " found at '", ToString(PATH_TO_WSTRING(found)), "'.");
    }

    return isStandalone;
}

} // namespace trace

#endif // OTEL_CLR_PROFILER_DEPLOYMENT_DETECTION_H_
