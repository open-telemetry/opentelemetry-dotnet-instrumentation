// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Provides extension points for configuring and interacting with the OpAMP client lifecycle.
/// </summary>
/// <remarks>
/// When multiple configured plugins implement this interface, only the first one in configuration
/// order is used for OpAMP. Other plugin interfaces implemented by the ignored OpAMP plugins are
/// unaffected.
/// </remarks>
public interface IOpAmpPlugin
{
    /// <summary>
    /// Allows modification of OpAMP client settings before the client is created.
    /// </summary>
    /// <param name="settings">The mutable settings used to configure the OpAMP client.</param>
    void ConfigureOpAmpOptions(OpAmpClientSettings settings);

    /// <summary>
    /// Allows the plugin to configure the constructed OpAMP client before its transport is started.
    /// </summary>
    /// <param name="client">The OpAMP client instance.</param>
    /// <remarks>
    /// This callback runs synchronously before <see cref="IPlugin.Initialized"/> and before the client
    /// transport starts. Register message listeners here to ensure they can observe the initial server
    /// response. The client may be retained for later use.
    /// </remarks>
    void ConfigureOpAmpClient(IOpAmpClient client);

    /// <summary>
    /// Called after the OpAMP client has been successfully started.
    /// </summary>
    /// <remarks>
    /// This callback is not called if preparation or startup fails, startup is cancelled, or shutdown
    /// begins before startup activation. If startup activation wins a race with shutdown, this callback
    /// completes before <see cref="BeforeOpAmpClientStopped"/> begins. Message listeners should be
    /// registered in <see cref="ConfigureOpAmpClient"/> rather than in this callback.
    /// </remarks>
    void AfterOpAmpClientStarted();

    /// <summary>
    /// Called before the OpAMP client is stopped, allowing plugins to release resources or stop work.
    /// </summary>
    /// <remarks>
    /// This callback may run after successful preparation even when startup fails, is cancelled, or
    /// shutdown suppresses <see cref="AfterOpAmpClientStarted"/>. Implementations must not assume the
    /// post-start callback ran and should release resources acquired during
    /// <see cref="ConfigureOpAmpClient"/> here. Automatic instrumentation bounds how long its loader
    /// waits for shutdown. During graceful shutdown, this callback completes before the client is
    /// disposed. If the callback does not complete before the shutdown deadline, forced cleanup may
    /// dispose the client before the callback returns. Implementations should return promptly and
    /// tolerate client operations failing after the deadline.
    /// </remarks>
    void BeforeOpAmpClientStopped();
}
