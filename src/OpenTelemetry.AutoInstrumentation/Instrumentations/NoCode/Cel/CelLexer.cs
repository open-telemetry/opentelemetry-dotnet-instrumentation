// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Lexical analyzer for CEL expressions.
/// </summary>
internal static class CelLexer
{
    public static List<CelToken> Tokenize(string input)
    {
        var tokens = new List<CelToken>();
        var position = 0;

        while (position < input.Length)
        {
            var c = input[position];

            // Skip whitespace
            if (char.IsWhiteSpace(c))
            {
                position++;
                continue;
            }

            // String literals
            if (c == '"' || c == '\'')
            {
                tokens.Add(ReadStringLiteral(input, ref position));
                continue;
            }

            // Numbers (including negative numbers)
            // Treat '-' as part of a negative number only if:
            // - It's at the start of the expression, OR
            // - The previous token is an operator (not a value like Number, Identifier, or RightParen/RightBracket)
            var canBeNegativeNumber = c == '-' && position + 1 < input.Length && char.IsDigit(input[position + 1]);
            if (canBeNegativeNumber)
            {
                CelTokenType? lastToken = tokens.Count > 0 ? tokens[tokens.Count - 1].Type : null;
                canBeNegativeNumber = lastToken is null
                    or CelTokenType.LeftParen
                    or CelTokenType.LeftBracket
                    or CelTokenType.Comma
                    or CelTokenType.Question
                    or CelTokenType.Colon
                    or CelTokenType.Equal
                    or CelTokenType.NotEqual
                    or CelTokenType.LessThan
                    or CelTokenType.LessThanOrEqual
                    or CelTokenType.GreaterThan
                    or CelTokenType.GreaterThanOrEqual
                    or CelTokenType.And
                    or CelTokenType.Or
                    or CelTokenType.Plus
                    or CelTokenType.Minus
                    or CelTokenType.Multiply
                    or CelTokenType.Divide
                    or CelTokenType.Modulo;
            }

            if (char.IsDigit(c) || canBeNegativeNumber)
            {
                tokens.Add(ReadNumber(input, ref position));
                continue;
            }

            // Identifiers and keywords
            if (char.IsLetter(c) || c == '_')
            {
                tokens.Add(ReadIdentifierOrKeyword(input, ref position));
                continue;
            }

            // Operators
            var token = ReadOperator(input, ref position);
            if (token.HasValue)
            {
                tokens.Add(token.Value);
                continue;
            }

            throw new InvalidOperationException($"Unexpected character '{c}' at position {position}");
        }

        tokens.Add(new CelToken(CelTokenType.EndOfInput, string.Empty, position));
        return tokens;
    }

    private static CelToken ReadStringLiteral(string input, ref int position)
    {
        var startPos = position;
        var quote = input[position];
        var sb = new StringBuilder();
        position++; // Skip opening quote

        while (position < input.Length)
        {
            var c = input[position];

            if (c == quote)
            {
                position++; // Skip closing quote
                return new CelToken(CelTokenType.String, sb.ToString(), startPos);
            }

            if (c == '\\')
            {
                if (position + 1 >= input.Length)
                {
                    throw new InvalidOperationException($"Invalid escape sequence at end of string at position {position}");
                }

                position++;
                var next = input[position];
                sb.Append(next switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => throw new InvalidOperationException($"Invalid escape sequence '\\{next}' at position {position}")
                });
                position++;
            }
            else
            {
                sb.Append(c);
                position++;
            }
        }

        throw new InvalidOperationException($"Unterminated string literal at position {startPos}");
    }

    private static CelToken ReadNumber(string input, ref int position)
    {
        var startPos = position;
        var sb = new StringBuilder();

        // Handle negative sign
        if (input[position] == '-')
        {
            sb.Append('-');
            position++;
        }

        // Read digits
        while (position < input.Length && (char.IsDigit(input[position]) || input[position] == '.'))
        {
            sb.Append(input[position]);
            position++;
        }

        return new CelToken(CelTokenType.Number, sb.ToString(), startPos);
    }

    private static CelToken ReadIdentifierOrKeyword(string input, ref int position)
    {
        var startPos = position;
        var sb = new StringBuilder();

        while (position < input.Length && (char.IsLetterOrDigit(input[position]) || input[position] == '_'))
        {
            sb.Append(input[position]);
            position++;
        }

        var value = sb.ToString();
        var type = value switch
        {
            "true" => CelTokenType.True,
            "false" => CelTokenType.False,
            "null" => CelTokenType.Null,
            _ => CelTokenType.Identifier
        };

        return new CelToken(type, value, startPos);
    }

    private static CelToken? ReadOperator(string input, ref int position)
    {
        var startPos = position;
        var c = input[position];

        // Two-character operators
        if (position + 1 < input.Length)
        {
            var twoChar = input.Substring(position, 2);
            var type = twoChar switch
            {
                "==" => CelTokenType.Equal,
                "!=" => CelTokenType.NotEqual,
                "<=" => CelTokenType.LessThanOrEqual,
                ">=" => CelTokenType.GreaterThanOrEqual,
                "&&" => CelTokenType.And,
                "||" => CelTokenType.Or,
                _ => (CelTokenType?)null
            };

            if (type.HasValue)
            {
                position += 2;
                return new CelToken(type.Value, twoChar, startPos);
            }
        }

        // Single-character operators
        var singleType = c switch
        {
            '.' => CelTokenType.Dot,
            '(' => CelTokenType.LeftParen,
            ')' => CelTokenType.RightParen,
            '[' => CelTokenType.LeftBracket,
            ']' => CelTokenType.RightBracket,
            ',' => CelTokenType.Comma,
            '?' => CelTokenType.Question,
            ':' => CelTokenType.Colon,
            '!' => CelTokenType.Not,
            '<' => CelTokenType.LessThan,
            '>' => CelTokenType.GreaterThan,
            '-' => CelTokenType.Minus,
            '+' => CelTokenType.Plus,
            '*' => CelTokenType.Multiply,
            '/' => CelTokenType.Divide,
            '%' => CelTokenType.Modulo,
            _ => (CelTokenType?)null
        };

        if (singleType.HasValue)
        {
            position++;
            return new CelToken(singleType.Value, c.ToString(), startPos);
        }

        return null;
    }
}
