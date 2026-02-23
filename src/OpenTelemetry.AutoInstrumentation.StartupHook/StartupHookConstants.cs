// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Constants related to startup hook isolation.
/// NOTE: This file is linked to the Loader project. When startup hook is removed,
/// delete this file and remove the link from Loader.csproj.
/// </summary>
internal static class StartupHookConstants
{
    /// <summary>
    /// Name of the isolated AssemblyLoadContext used by startup hook.
    /// </summary>
    public const string IsolatedAssemblyLoadContextName = "OpenTelemetry.AutoInstrumentation.IsolatedAssemblyLoadContext";
}
