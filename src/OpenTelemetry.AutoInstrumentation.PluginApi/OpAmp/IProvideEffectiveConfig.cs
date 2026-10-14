// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Provides effective configuration for OpAMP reporting.
/// </summary>
/// <remarks>
/// The selected plugin must enable effective configuration reporting in
/// <see cref="IOpAmpPlugin.ConfigureOpAmpOptions"/>.
/// </remarks>
public interface IProvideEffectiveConfig : IOpAmpPlugin
{
    /// <summary>
    /// Gets the complete effective configuration currently used by the plugin.
    /// </summary>
    /// <returns>The configuration files, or an empty collection to report an empty configuration.</returns>
    /// <remarks>
    /// File names must be unique. Contents are sent without redaction. This read-only callback
    /// runs on the thread requesting a report and must return promptly. Getter calls are serialized but
    /// may overlap plugin lifecycle callbacks and configuration updates. Publish configuration
    /// safely across threads and return a stable snapshot, including its buffers.
    /// Do not issue reporting notifications or synchronously wait for OpAMP callbacks from this getter.
    /// A running callback may overlap plugin cleanup.
    /// </remarks>
    IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig();
}
