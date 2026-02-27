// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Token types for CEL expressions.
/// </summary>
internal enum CelTokenType
{
    // Literals
    String,
    Number,
    True,
    False,
    Null,

    // Identifiers and keywords
    Identifier,

    // Operators
    Dot,                // .
    LeftParen,          // (
    RightParen,         // )
    LeftBracket,        // [
    RightBracket,       // ]
    Comma,              // ,
    Question,           // ?
    Colon,              // :
    Not,                // !
    Equal,              // ==
    NotEqual,           // !=
    LessThan,           // <
    LessThanOrEqual,    // <=
    GreaterThan,        // >
    GreaterThanOrEqual, // >=
    And,                // &&
    Or,                 // ||
    Minus,              // -
    Plus,               // +
    Multiply,           // *
    Divide,             // /
    Modulo,             // %

    // Special
    EndOfInput
}

/// <summary>
/// Represents a token in a CEL expression.
/// </summary>
internal readonly struct CelToken
{
    public CelToken(CelTokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public CelTokenType Type { get; }

    public string Value { get; }

    public int Position { get; }
}
