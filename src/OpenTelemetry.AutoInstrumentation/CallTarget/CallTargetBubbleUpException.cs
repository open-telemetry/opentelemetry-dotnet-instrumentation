// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Runtime.Serialization;

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

/// <summary>
/// Base exception for exceptions thrown by CallTarget integrations that must bypass the
/// CallTarget exception handler and propagate to the instrumented application.
/// </summary>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class CallTargetBubbleUpException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallTargetBubbleUpException"/> class.
    /// </summary>
    public CallTargetBubbleUpException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CallTargetBubbleUpException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public CallTargetBubbleUpException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CallTargetBubbleUpException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public CallTargetBubbleUpException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CallTargetBubbleUpException"/> class.
    /// </summary>
    /// <param name="info">The serialized object data.</param>
    /// <param name="context">Context about the source or destination.</param>
#pragma warning disable SYSLIB0051 // Type or member is obsolete. Retained for parity with the standard exception constructors on .NET Framework.
    public CallTargetBubbleUpException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051 // Type or member is obsolete
}
