// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Provides extension points for configuring and interacting with the OpAMP client lifecycle.
/// </summary>
public interface IOpAmpPlugin
{
    /// <summary>
    /// Allows modification of OpAMP client settings before the client is created.
    /// </summary>
    /// <param name="settings">The mutable settings used to configure the OpAMP client.</param>
    void ConfigureOpAmpOptions(OpAmpClientSettings settings);

    /// <summary>
    /// Called after the OpAMP client has been successfully started.
    /// </summary>
    /// <param name="client">The running OpAMP client instance.</param>
    void AfterOpAmpClientStarted(OpAmpClient client);

    /// <summary>
    /// Called before the OpAMP client is stopped, allowing plugins to release resources or stop work.
    /// </summary>
    void BeforeOpAmpClientStopped();
}
