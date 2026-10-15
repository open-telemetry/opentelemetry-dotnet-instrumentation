// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Provides plugin access to the OpAMP client managed by automatic instrumentation.
/// </summary>
public interface IOpAmpClient
{
    /// <summary>
    /// Notifies automatic instrumentation of an effective configuration change.
    /// </summary>
    /// <remarks>
    /// Call when the effective configuration changes.
    /// Repeated notifications can resend unchanged configuration.
    /// When reporting is enabled and the server accepts effective configuration, the selected
    /// plugin's <see cref="IProvideEffectiveConfig.GetEffectiveConfig"/> must be able to complete
    /// while this notification is in progress. Automatic instrumentation also requests current
    /// configuration when reporting becomes available or the server requests full state.
    /// Returning from this notification does not confirm delivery to the server.
    /// Do not rely on delivery during shutdown.
    /// </remarks>
    void NotifyEffectiveConfigChanged();

    /// <summary>
    /// Subscribes a listener to messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The OpAMP message type.</typeparam>
    /// <param name="listener">The listener to subscribe.</param>
    /// <remarks>
    /// Subscribe during <see cref="IOpAmpPlugin.ConfigureOpAmpClient"/> to receive messages from the
    /// initial server response. Listener callbacks must return promptly.
    /// </remarks>
    void Subscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage;

    /// <summary>
    /// Unsubscribes a listener from messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The OpAMP message type.</typeparam>
    /// <param name="listener">The listener to unsubscribe.</param>
    void Unsubscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage;
}
