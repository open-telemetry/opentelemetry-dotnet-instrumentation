// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Provides effective configuration for OpAMP reporting.
/// </summary>
/// <remarks>
/// Implementing this interface on the selected <see cref="IOpAmpPlugin"/> makes effective
/// configuration reporting available. The plugin must explicitly enable it in
/// <see cref="IOpAmpPlugin.ConfigureOpAmpOptions"/>.
/// </remarks>
public interface IProvideEffectiveConfig : IOpAmpPlugin
{
    /// <summary>
    /// Gets the effective configuration currently used by the plugin.
    /// </summary>
    /// <returns>The complete effective configuration.</returns>
    /// <remarks>
    /// The configuration can contain at most 16 files, and each file can contain at most 512 KiB.
    /// File names must be ordinally unique. An empty file name is valid, including when the configuration
    /// contains multiple files. Automatic instrumentation does not redact file contents before sending
    /// them. The callback should return promptly.
    /// </remarks>
    IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig();
}
