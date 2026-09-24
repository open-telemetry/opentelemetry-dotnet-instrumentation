// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

namespace OpenTelemetry.AutoInstrumentation.FuzzTests.Instrumentations.NoCode.Cel;

public class CelLexerFuzzTests
{
    private const int MaxTests = 500;

    private static readonly Gen<char> InputCharacterGenerator = Gen.OneOf(
        Gen.Choose(0x20, 0x7E).Select(value => (char)value),
        Gen.Elements('\0', '\t', '\r', '\n', '\\', '\'', '"'),
        Gen.Choose(char.MinValue, char.MaxValue).Select(value => (char)value));

    private static readonly Gen<char> IdentifierCharacterGenerator = Gen.OneOf(
        Gen.Choose('a', 'z').Select(value => (char)value),
        Gen.Choose('A', 'Z').Select(value => (char)value),
        Gen.Choose('0', '9').Select(value => (char)value),
        Gen.Constant('_'));

    private static readonly Gen<string> WhitespaceGenerator =
        Gen.Elements(" ", "\t", "\r\n", " \t ");

    private static readonly Gen<ExpectedLexeme> OperatorGenerator = Gen.Elements(
        new ExpectedLexeme(".", CelTokenType.Dot, "."),
        new ExpectedLexeme("(", CelTokenType.LeftParen, "("),
        new ExpectedLexeme(")", CelTokenType.RightParen, ")"),
        new ExpectedLexeme("[", CelTokenType.LeftBracket, "["),
        new ExpectedLexeme("]", CelTokenType.RightBracket, "]"),
        new ExpectedLexeme(",", CelTokenType.Comma, ","),
        new ExpectedLexeme("?", CelTokenType.Question, "?"),
        new ExpectedLexeme(":", CelTokenType.Colon, ":"),
        new ExpectedLexeme("!", CelTokenType.Not, "!"),
        new ExpectedLexeme("==", CelTokenType.Equal, "=="),
        new ExpectedLexeme("!=", CelTokenType.NotEqual, "!="),
        new ExpectedLexeme("<", CelTokenType.LessThan, "<"),
        new ExpectedLexeme("<=", CelTokenType.LessThanOrEqual, "<="),
        new ExpectedLexeme(">", CelTokenType.GreaterThan, ">"),
        new ExpectedLexeme(">=", CelTokenType.GreaterThanOrEqual, ">="),
        new ExpectedLexeme("&&", CelTokenType.And, "&&"),
        new ExpectedLexeme("||", CelTokenType.Or, "||"),
        new ExpectedLexeme("-", CelTokenType.Minus, "-"),
        new ExpectedLexeme("+", CelTokenType.Plus, "+"),
        new ExpectedLexeme("*", CelTokenType.Multiply, "*"),
        new ExpectedLexeme("/", CelTokenType.Divide, "/"),
        new ExpectedLexeme("%", CelTokenType.Modulo, "%"));

    private static readonly Gen<ExpectedLexeme> LexemeGenerator = Gen.OneOf(
        Gen.Elements(
            new ExpectedLexeme("true", CelTokenType.True, "true"),
            new ExpectedLexeme("false", CelTokenType.False, "false"),
            new ExpectedLexeme("null", CelTokenType.Null, "null")),
        CreateIdentifierGenerator(),
        CreateIntegerGenerator(),
        CreateDecimalGenerator(),
        CreateStringGenerator(),
        OperatorGenerator);

    private static readonly Arbitrary<string> ArbitraryInputs = Gen.Sized(size =>
        from length in Gen.Choose(0, Math.Min((size * 2) + 1, 128))
        from characters in Gen.ArrayOf(InputCharacterGenerator, length)
        select new string(characters)).ToArbitrary();

    private static readonly Arbitrary<TokenSequence> TokenSequences =
        (from length in Gen.Choose(1, 32)
         from lexemes in Gen.ArrayOf(LexemeGenerator, length)
         from whitespace in Gen.ArrayOf(WhitespaceGenerator, length + 1)
         select TokenSequence.Create(lexemes, whitespace)).ToArbitrary();

    private static readonly Arbitrary<NumberPair> NumberPairs =
        (from left in Gen.Choose(0, 1_000_000)
         from right in Gen.Choose(0, 1_000_000)
         from whitespace in WhitespaceGenerator
         select new NumberPair(left, right, whitespace)).ToArbitrary();

    [Property(MaxTest = MaxTests)]
    public Property Tokenize_ArbitraryInput_HasStableResultOrExpectedLexicalFailure()
    {
        return Prop.ForAll(ArbitraryInputs, input =>
        {
            List<CelToken> firstResult;
            try
            {
                firstResult = CelLexer.Tokenize(input);
            }
            catch (InvalidOperationException)
            {
                // Unexpected characters, invalid escapes, and unterminated strings are valid lexer rejections.
                return;
            }

            var secondResult = CelLexer.Tokenize(input);

            Assert.Equal(firstResult.Count, secondResult.Count);
            Assert.NotEmpty(firstResult);

            var previousPosition = 0;
            for (var i = 0; i < firstResult.Count; i++)
            {
                var firstToken = firstResult[i];
                var secondToken = secondResult[i];

                Assert.Equal(firstToken.Type, secondToken.Type);
                Assert.Equal(firstToken.Value, secondToken.Value);
                Assert.Equal(firstToken.Position, secondToken.Position);
                Assert.InRange(firstToken.Position, previousPosition, input.Length);

                previousPosition = firstToken.Position;
            }

            var endOfInput = firstResult[firstResult.Count - 1];
            Assert.Equal(CelTokenType.EndOfInput, endOfInput.Type);
            Assert.Equal(string.Empty, endOfInput.Value);
            Assert.Equal(input.Length, endOfInput.Position);
        });
    }

    [Property(MaxTest = MaxTests)]
    public Property Tokenize_GeneratedLexemeSequence_PreservesTokens()
    {
        return Prop.ForAll(TokenSequences, testCase =>
        {
            var actual = CelLexer.Tokenize(testCase.Source);

            Assert.Equal(testCase.ExpectedTokens.Length + 1, actual.Count);
            for (var i = 0; i < testCase.ExpectedTokens.Length; i++)
            {
                var expected = testCase.ExpectedTokens[i];
                Assert.Equal(expected.Type, actual[i].Type);
                Assert.Equal(expected.Value, actual[i].Value);
                Assert.Equal(expected.Position, actual[i].Position);
            }

            var endOfInput = actual[actual.Count - 1];
            Assert.Equal(CelTokenType.EndOfInput, endOfInput.Type);
            Assert.Equal(testCase.Source.Length, endOfInput.Position);
        });
    }

    [Property(MaxTest = MaxTests)]
    public Property Tokenize_MinusSign_UsesPreviousTokenContext()
    {
        return Prop.ForAll(NumberPairs, testCase =>
        {
            var left = testCase.Left.ToString(CultureInfo.InvariantCulture);
            var right = testCase.Right.ToString(CultureInfo.InvariantCulture);

            var negativeNumber = CelLexer.Tokenize($"-{right}");
            Assert.Equal(2, negativeNumber.Count);
            Assert.Equal(CelTokenType.Number, negativeNumber[0].Type);
            Assert.Equal($"-{right}", negativeNumber[0].Value);

            var subtraction = CelLexer.Tokenize($"{left}{testCase.Whitespace}-{right}");
            Assert.Equal(4, subtraction.Count);
            Assert.Equal(CelTokenType.Number, subtraction[0].Type);
            Assert.Equal(left, subtraction[0].Value);
            Assert.Equal(CelTokenType.Minus, subtraction[1].Type);
            Assert.Equal(CelTokenType.Number, subtraction[2].Type);
            Assert.Equal(right, subtraction[2].Value);

            var numberAfterOperator = CelLexer.Tokenize($"+{testCase.Whitespace}-{right}");
            Assert.Equal(3, numberAfterOperator.Count);
            Assert.Equal(CelTokenType.Plus, numberAfterOperator[0].Type);
            Assert.Equal(CelTokenType.Number, numberAfterOperator[1].Type);
            Assert.Equal($"-{right}", numberAfterOperator[1].Value);
        });
    }

    private static Gen<ExpectedLexeme> CreateIdentifierGenerator()
    {
        return from length in Gen.Choose(0, 16)
               from characters in Gen.ArrayOf(IdentifierCharacterGenerator, length)
               let identifier = $"_{new string(characters)}"
               select new ExpectedLexeme(identifier, CelTokenType.Identifier, identifier);
    }

    private static Gen<ExpectedLexeme> CreateIntegerGenerator()
    {
        return Gen.Choose(0, 1_000_000)
            .Select(value => value.ToString(CultureInfo.InvariantCulture))
            .Select(value => new ExpectedLexeme(value, CelTokenType.Number, value));
    }

    private static Gen<ExpectedLexeme> CreateDecimalGenerator()
    {
        return from whole in Gen.Choose(0, 10_000)
               from fractional in Gen.Choose(0, 999)
               let value = $"{whole.ToString(CultureInfo.InvariantCulture)}.{fractional.ToString("D3", CultureInfo.InvariantCulture)}"
               select new ExpectedLexeme(value, CelTokenType.Number, value);
    }

    private static Gen<ExpectedLexeme> CreateStringGenerator()
    {
        return from length in Gen.Choose(0, 32)
               from characters in Gen.ArrayOf(InputCharacterGenerator, length)
               from quote in Gen.Elements('\'', '"')
               let value = new string(characters)
               select new ExpectedLexeme(Quote(value, quote), CelTokenType.String, value);
    }

    private static string Quote(string value, char quote)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append(quote);

        foreach (var character in value)
        {
            switch (character)
            {
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\'':
                    builder.Append("\\'");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        builder.Append(quote);
        return builder.ToString();
    }

    private sealed class ExpectedLexeme
    {
        public ExpectedLexeme(string source, CelTokenType type, string value)
        {
            Source = source;
            Type = type;
            Value = value;
        }

        public string Source { get; }

        public CelTokenType Type { get; }

        public string Value { get; }
    }

    private sealed class ExpectedToken
    {
        public ExpectedToken(CelTokenType type, string value, int position)
        {
            Type = type;
            Value = value;
            Position = position;
        }

        public CelTokenType Type { get; }

        public string Value { get; }

        public int Position { get; }
    }

    private sealed class TokenSequence
    {
        private TokenSequence(string source, ExpectedToken[] expectedTokens)
        {
            Source = source;
            ExpectedTokens = expectedTokens;
        }

        public string Source { get; }

        public ExpectedToken[] ExpectedTokens { get; }

        public static TokenSequence Create(ExpectedLexeme[] lexemes, string[] whitespace)
        {
            var source = new StringBuilder();
            var expectedTokens = new ExpectedToken[lexemes.Length];

            source.Append(whitespace[0]);
            for (var i = 0; i < lexemes.Length; i++)
            {
                var lexeme = lexemes[i];
                expectedTokens[i] = new ExpectedToken(lexeme.Type, lexeme.Value, source.Length);
                source.Append(lexeme.Source);
                source.Append(whitespace[i + 1]);
            }

            return new TokenSequence(source.ToString(), expectedTokens);
        }

        public override string ToString() => Source;
    }

    private sealed class NumberPair
    {
        public NumberPair(int left, int right, string whitespace)
        {
            Left = left;
            Right = right;
            Whitespace = whitespace;
        }

        public int Left { get; }

        public int Right { get; }

        public string Whitespace { get; }

        public override string ToString() => $"{Left}{Whitespace}-{Right}";
    }
}
