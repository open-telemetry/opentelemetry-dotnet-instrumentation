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
    /// Subscribes a listener to messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The OpAMP message type.</typeparam>
    /// <param name="listener">The listener to subscribe.</param>
    /// <remarks>
    /// Subscribe during <see cref="IOpAmpPlugin.ConfigureOpAmpClient"/> to observe messages from the
    /// initial server response. Listener callbacks run on the upstream client's dispatch path and
    /// should return promptly; move longer work to bounded plugin-owned processing.
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
