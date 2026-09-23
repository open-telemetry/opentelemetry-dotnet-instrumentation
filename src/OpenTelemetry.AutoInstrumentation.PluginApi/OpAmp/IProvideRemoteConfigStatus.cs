// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Provides remote configuration status for OpAMP reporting.
/// </summary>
/// <remarks>
/// Implementing this interface on the selected <see cref="IOpAmpPlugin"/> makes remote
/// configuration status reporting available. The plugin must explicitly enable it in
/// <see cref="IOpAmpPlugin.ConfigureOpAmpOptions"/>.
/// </remarks>
public interface IProvideRemoteConfigStatus : IOpAmpPlugin
{
    /// <summary>
    /// Gets the status of the last remote configuration processed by the plugin.
    /// </summary>
    /// <returns>
    /// The remote configuration status, or <see langword="null"/> when no remote configuration has
    /// been processed.
    /// </returns>
    /// <remarks>The callback should return promptly.</remarks>
    RemoteConfigStatusReport? GetRemoteConfigStatus();
}
