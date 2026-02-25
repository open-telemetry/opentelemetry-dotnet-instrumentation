/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "standalone_deployment_detection.h"

#include <filesystem>

#include "environment_variables.h"
#include "logger.h"
#include "util.h"

namespace trace
{

bool IsStandaloneDeployment()
{
    static int sValue = -1;
    if (sValue != -1)
    {
        return sValue == 1;
    }

    const auto homePath = GetEnvironmentValue(environment::profiler_home_path);
    if (homePath.empty())
    {
        Logger::Debug("IsStandaloneDeployment: OTEL_DOTNET_AUTO_HOME is not set, assuming NOT standalone deployment.");
        sValue = 0;
        return false;
    }

    const auto homeDir = std::filesystem::path(ToString(homePath));

    // Standalone deployment has net/ (and netfx/ on Windows) subdirectories
    // under OTEL_DOTNET_AUTO_HOME. Non-standalone deployments (e.g., NuGet-based) do not.
    const bool hasNetDir = std::filesystem::is_directory(homeDir / "net");

#ifdef _WIN32
    const bool hasNetfxDir = std::filesystem::is_directory(homeDir / "netfx");
    const bool isStandalone = hasNetDir || hasNetfxDir;
#else
    const bool isStandalone = hasNetDir;
#endif

    if (!isStandalone)
    {
        Logger::Info("Detected non-standalone deployment (e.g., NuGet-based): no net/netfx subdirectories under OTEL_DOTNET_AUTO_HOME.");
    }

    sValue = isStandalone ? 1 : 0;
    return sValue == 1;
}

} // namespace trace
