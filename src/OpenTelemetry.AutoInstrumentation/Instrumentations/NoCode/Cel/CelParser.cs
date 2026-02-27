// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Recursive descent parser for CEL expressions.
/// </summary>
internal sealed class CelParser
{
    private readonly List<CelToken> _tokens;
    private int _position;

    public CelParser(List<CelToken> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    private CelToken Current => _position < _tokens.Count ? _tokens[_position] : _tokens[_tokens.Count - 1];

    public CelNode Parse()
    {
        var expr = ParseTernary();
        if (Current.Type != CelTokenType.EndOfInput)
        {
            throw new InvalidOperationException($"Unexpected token {Current.Type} at position {Current.Position}");
        }

        return expr;
    }

    private CelToken Consume(CelTokenType? expectedType = null)
    {
        var token = Current;
        if (expectedType.HasValue && token.Type != expectedType.Value)
        {
            throw new InvalidOperationException($"Expected token type {expectedType.Value} but got {token.Type} at position {token.Position}");
        }

        _position++;
        return token;
    }

    private bool Match(params CelTokenType[] types)
    {
        foreach (var type in types)
        {
            if (Current.Type == type)
            {
                return true;
            }
        }

        return false;
    }

    private CelNode ParseTernary()
    {
        var expr = ParseLogicalOr();

        if (Match(CelTokenType.Question))
        {
            Consume();
            var trueExpr = ParseTernary();
            Consume(CelTokenType.Colon);
            var falseExpr = ParseTernary();
            return new CelTernaryNode(expr, trueExpr, falseExpr);
        }

        return expr;
    }

    private CelNode ParseLogicalOr()
    {
        var left = ParseLogicalAnd();

        while (Match(CelTokenType.Or))
        {
            var op = Consume();
            var right = ParseLogicalAnd();
            left = new CelBinaryOperatorNode(left, op.Value, right);
        }

        return left;
    }

    private CelNode ParseLogicalAnd()
    {
        var left = ParseEquality();

        while (Match(CelTokenType.And))
        {
            var op = Consume();
            var right = ParseEquality();
            left = new CelBinaryOperatorNode(left, op.Value, right);
        }

        return left;
    }

    private CelNode ParseEquality()
    {
        var left = ParseRelational();

        while (Match(CelTokenType.Equal, CelTokenType.NotEqual))
        {
            var op = Consume();
            var right = ParseRelational();
            left = new CelBinaryOperatorNode(left, op.Value, right);
        }

        return left;
    }

    private CelNode ParseRelational()
    {
        var left = ParseAdditive();

        while (Match(CelTokenType.LessThan, CelTokenType.LessThanOrEqual, CelTokenType.GreaterThan, CelTokenType.GreaterThanOrEqual))
        {
            var op = Consume();
            var right = ParseAdditive();
            left = new CelBinaryOperatorNode(left, op.Value, right);
        }

        return left;
    }

    private CelNode ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Match(CelTokenType.Plus, CelTokenType.Minus))
        {
            var op = Consume();
            var right = ParseMultiplicative();
            left = new CelBinaryOperatorNode(left, op.Value, right);
        }

        return left;
    }

    private CelNode ParseMultiplicative()
    {
        var left = ParseUnary();

        while (Match(CelTokenType.Multiply, CelTokenType.Divide, CelTokenType.Modulo))
        {
            var op = Consume();
            var right = ParseUnary();
            left = new CelBinaryOperatorNode(left, op.Value, right);
        }

        return left;
    }

    private CelNode ParseUnary()
    {
        if (Match(CelTokenType.Not, CelTokenType.Minus))
        {
            var op = Consume();
            var operand = ParseUnary();
            return new CelUnaryOperatorNode(op.Value, operand);
        }

        return ParsePostfix();
    }

    private CelNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Match(CelTokenType.Dot))
            {
                Consume();
                var memberName = Consume(CelTokenType.Identifier).Value;
                expr = new CelMemberAccessNode(expr, memberName);
            }
            else if (Match(CelTokenType.LeftBracket))
            {
                Consume();
                var index = ParseTernary();
                Consume(CelTokenType.RightBracket);
                expr = new CelIndexAccessNode(expr, index);
            }
            else if (Match(CelTokenType.LeftParen) && expr is CelIdentifierNode identNode)
            {
                // Function call
                Consume();
                var args = new List<CelNode>();

                if (!Match(CelTokenType.RightParen))
                {
                    args.Add(ParseTernary());
                    while (Match(CelTokenType.Comma))
                    {
                        Consume();
                        args.Add(ParseTernary());
                    }
                }

                Consume(CelTokenType.RightParen);
                expr = new CelFunctionCallNode(identNode.RawExpression, args.ToArray());
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private CelNode ParsePrimary()
    {
        if (Match(CelTokenType.True))
        {
            Consume();
            return new CelLiteralNode(true);
        }

        if (Match(CelTokenType.False))
        {
            Consume();
            return new CelLiteralNode(false);
        }

        if (Match(CelTokenType.Null))
        {
            Consume();
            return new CelLiteralNode(null);
        }

        if (Match(CelTokenType.Number))
        {
            var token = Consume();
#if NET
            if (token.Value.Contains('.', StringComparison.Ordinal))
#else
            if (token.Value.IndexOf('.') >= 0)
#endif
            {
                return new CelLiteralNode(double.Parse(token.Value, CultureInfo.InvariantCulture));
            }
            else
            {
                return new CelLiteralNode(int.Parse(token.Value, CultureInfo.InvariantCulture));
            }
        }

        if (Match(CelTokenType.String))
        {
            var token = Consume();
            return new CelLiteralNode(token.Value);
        }

        if (Match(CelTokenType.Identifier))
        {
            var token = Consume();
            return new CelIdentifierNode(token.Value);
        }

        if (Match(CelTokenType.LeftParen))
        {
            Consume();
            var expr = ParseTernary();
            Consume(CelTokenType.RightParen);
            return expr;
        }

        throw new InvalidOperationException($"Unexpected token {Current.Type} at position {Current.Position}");
    }
}
