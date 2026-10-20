// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Provides remote configuration status for OpAMP reporting.
/// </summary>
/// <remarks>
/// The selected plugin must enable remote configuration status reporting in
/// <see cref="IOpAmpPlugin.ConfigureOpAmpOptions"/>.
/// </remarks>
public interface IProvideRemoteConfigStatus : IOpAmpPlugin
{
    /// <summary>
    /// Gets the current application status of the last remote configuration received by the plugin.
    /// </summary>
    /// <returns>The remote configuration status, or <see langword="null"/> when no status is available.</returns>
    /// <remarks>
    /// This getter may run before any configuration is received or before application completes.
    /// Return <see langword="null"/> until a status is available, and report
    /// <see cref="RemoteConfigStatusCode.Applying"/> with the received configuration's hash while applying it.
    /// This read-only callback must return promptly and may be invoked while a reporting notification
    /// is in progress. Getter calls may overlap plugin lifecycle callbacks, including cleanup, and
    /// configuration updates. Publish status safely across threads and return a stable snapshot,
    /// including its hash buffer.
    /// Do not issue reporting notifications or synchronously wait for OpAMP callbacks from this getter.
    /// </remarks>
    RemoteConfigStatusReport? GetRemoteConfigStatus();
}
