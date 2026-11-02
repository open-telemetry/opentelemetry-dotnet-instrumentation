// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Provides plugin access to the OpAMP client managed by automatic instrumentation.
/// </summary>
/// <remarks>
/// Reporting notifications asynchronously invalidate current provider state and may be coalesced.
/// Publish a stable snapshot before notifying. Automatic instrumentation
/// manages server capability timing.
/// </remarks>
public interface IOpAmpClient
{
    /// <summary>
    /// Notifies automatic instrumentation of an effective configuration change.
    /// </summary>
    /// <remarks>
    /// Call from <see cref="IOpAmpPlugin.AfterOpAmpClientStarted"/> after publishing the initial
    /// effective configuration, and whenever it later changes.
    /// Notifications are handled asynchronously and may be coalesced. If effective configuration
    /// reporting is unavailable, a notification remains pending until it becomes available.
    /// Later notifications can resend unchanged configuration.
    /// Automatic instrumentation may call the selected plugin's
    /// <see cref="IProvideEffectiveConfig.GetEffectiveConfig"/> in response to this notification
    /// or a server-requested full-state report.
    /// A failed read or send is not retried automatically; a later effective-configuration notification
    /// or full-state request can retry it.
    /// Returning from this notification does not confirm delivery to the server.
    /// Do not rely on delivery during shutdown.
    /// </remarks>
    void NotifyEffectiveConfigChanged();

    /// <summary>
    /// Notifies automatic instrumentation that the remote configuration status may have changed.
    /// </summary>
    /// <remarks>
    /// If a status is already available, call from
    /// <see cref="IOpAmpPlugin.AfterOpAmpClientStarted"/> after publishing it. Otherwise, call when
    /// the first status becomes available and whenever it later changes. Notifications are handled
    /// asynchronously and may be coalesced. Later notifications can resend unchanged status.
    /// Reporting continues if the server stops offering remote configuration.
    /// Automatic instrumentation may call the selected plugin's
    /// <see cref="IProvideRemoteConfigStatus.GetRemoteConfigStatus"/> in response to this notification.
    /// The provider may be queried independently for a server-requested full-state report.
    /// A failed read or send is not retried automatically; a later remote-status notification or
    /// full-state request can retry it.
    /// Returning from this notification does not confirm delivery to the server.
    /// Do not rely on delivery during shutdown.
    /// </remarks>
    void NotifyRemoteConfigStatusChanged();

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
