// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Provides extension points for configuring and interacting with the OpAMP client lifecycle.
/// </summary>
/// <remarks>
/// Only the first configured implementation is used for OpAMP. Ignored implementations still
/// participate through their other plugin interfaces. Lifecycle callbacks must return promptly
/// and tolerate client disposal during forced shutdown.
/// </remarks>
public interface IOpAmpPlugin
{
    /// <summary>
    /// Allows modification of OpAMP client settings before the client is created.
    /// </summary>
    /// <param name="settings">The mutable settings used to configure the OpAMP client.</param>
    /// <remarks>
    /// Settings may only be mutated during this callback. Unsupported capabilities are not advertised.
    /// </remarks>
    void ConfigureOpAmpOptions(OpAmpClientSettings settings);

    /// <summary>
    /// Allows the plugin to configure the OpAMP client before it starts.
    /// </summary>
    /// <param name="client">The OpAMP client instance.</param>
    /// <remarks>
    /// This callback runs before <see cref="IPlugin.Initialized"/>. Register listeners here to receive
    /// messages from the initial server response. The client may be retained for later use.
    /// </remarks>
    void ConfigureOpAmpClient(IOpAmpClient client);

    /// <summary>
    /// Called after the OpAMP client's start operation completes.
    /// </summary>
    /// <remarks>
    /// This does not guarantee server connectivity or successful initial-message delivery. The callback
    /// is skipped if preparation fails, the start operation throws or is cancelled, or shutdown begins
    /// first. When invoked, it completes before <see cref="BeforeOpAmpClientStopped"/> begins.
    /// </remarks>
    void AfterOpAmpClientStarted();

    /// <summary>
    /// Called before the OpAMP client stops.
    /// </summary>
    /// <remarks>
    /// This callback may run without <see cref="AfterOpAmpClientStarted"/>. Release resources acquired
    /// during <see cref="ConfigureOpAmpClient"/> here, including when that callback throws.
    /// During graceful cleanup, it completes before the client is disposed.
    /// </remarks>
    void BeforeOpAmpClientStopped();
}
