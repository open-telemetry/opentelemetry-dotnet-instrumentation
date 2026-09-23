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

    /// <summary>
    /// Reports the complete set of custom capabilities supported by the plugin.
    /// </summary>
    /// <param name="capabilities">
    /// The case-sensitive reverse fully qualified domain names of the supported capabilities.
    /// Passing an empty collection clears all previously reported custom capabilities.
    /// </param>
    /// <remarks>
    /// Each call replaces the previously reported set. Duplicate entries and ordering do not
    /// constitute a change. Calls made before the OpAMP client starts retain only the latest set;
    /// a nonempty set is reported after startup succeeds. Later changes that have not yet been
    /// submitted may be coalesced to the latest replacement state.
    /// </remarks>
    void ReportCustomCapabilities(IReadOnlyCollection<string> capabilities);

    /// <summary>
    /// Notifies automatic instrumentation that the effective configuration may have changed.
    /// </summary>
    /// <remarks>
    /// When effective configuration reporting was enabled during client configuration, the selected
    /// plugin's <see cref="IProvideEffectiveConfig.GetEffectiveConfig"/> method is called asynchronously
    /// while the server supports receiving effective configuration. Its result replaces the previously
    /// observed effective configuration, and a partial report is queued only when that state changes.
    /// Server support becoming available also causes a refresh. Multiple pending notifications may be
    /// coalesced; unchanged state is resent only in response to a full-state request.
    /// </remarks>
    void NotifyEffectiveConfigChanged();

    /// <summary>
    /// Notifies automatic instrumentation that the remote configuration status may have changed.
    /// </summary>
    /// <remarks>
    /// When remote configuration status reporting was enabled during client configuration, the selected
    /// plugin's <see cref="IProvideRemoteConfigStatus.GetRemoteConfigStatus"/> method is called
    /// asynchronously while the server supports remote configuration. Its result replaces the previously
    /// observed remote configuration status, and a partial report is queued only when that state changes.
    /// Server support becoming available also causes a refresh. Multiple pending notifications may be
    /// coalesced; unchanged state is resent only in response to a full-state request.
    /// </remarks>
    void NotifyRemoteConfigStatusChanged();

    /// <summary>
    /// Sends a custom OpAMP message when the client and server both advertise its capability.
    /// </summary>
    /// <param name="capability">The case-sensitive custom capability associated with the message.</param>
    /// <param name="type">The message type within the custom capability.</param>
    /// <param name="data">The message payload.</param>
    /// <remarks>
    /// The capability must already have been submitted to the upstream OpAMP client. If a preceding
    /// <see cref="ReportCustomCapabilities"/> call is still pending, await <see cref="FlushAsync"/>
    /// before retrying the message. Rejected messages are not retried automatically.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The upstream client's pending custom-message count or payload-size limit would be exceeded.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The manager-owned upstream client has already been disposed.
    /// </exception>
    void SendCustomMessage(string capability, string type, ReadOnlyMemory<byte> data);

    /// <summary>
    /// Flushes the resulting state from reports accepted before this call and messages queued by the
    /// upstream OpAMP client.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel waiting for pending work.</param>
    /// <returns>A task representing the flush operation.</returns>
    /// <exception cref="OperationCanceledException">
    /// The operation was cancelled through <paramref name="cancellationToken"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The OpAMP client has not started successfully or shutdown has begun.
    /// </exception>
    /// <remarks>
    /// A successful flush establishes submission of previously accepted custom-capability changes.
    /// It does not retry custom messages that were rejected before those capabilities were submitted.
    /// </remarks>
    Task FlushAsync(CancellationToken cancellationToken);
}
