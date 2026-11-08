// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Serilog.Bridge;
using OpenTelemetry.Trace;
using Serilog.Events;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class SerilogTests
{
    // TODO: Remove when Logs Api is made public in non-rc builds.
    private static readonly Type OpenTelemetryLogSeverityType = typeof(Tracer).Assembly.GetType("OpenTelemetry.Logs.LogRecordSeverity")!;

    public static TheoryData<int, int> GetData()
    {
        var theoryData = new TheoryData<int, int>
        {
            { (int)LogEventLevel.Verbose, GetOpenTelemetrySeverityValue("Trace") },
            { (int)LogEventLevel.Debug, GetOpenTelemetrySeverityValue("Debug") },
            { (int)LogEventLevel.Information, GetOpenTelemetrySeverityValue("Info") },
            { (int)LogEventLevel.Warning, GetOpenTelemetrySeverityValue("Warn") },
            { (int)LogEventLevel.Error, GetOpenTelemetrySeverityValue("Error") },
            { (int)LogEventLevel.Fatal, GetOpenTelemetrySeverityValue("Fatal") }
        };

        return theoryData;
    }

    [Theory]
    [MemberData(nameof(GetData))]
    public void BuiltinSerilogLevelValues_AreMapped(int serilogLevelValue, int expectedOpenTelemetrySeverity)
    {
        Assert.Equal(expectedOpenTelemetrySeverity, OpenTelemetrySerilogSink.MapLogLevel(serilogLevelValue));
    }

    [Fact]
    public void StructureValue_IsConvertedToKeyValuePairs()
    {
        var value = new StructureValue(
            [new LogEventProperty("X", new ScalarValue(1)), new LogEventProperty("Label", new ScalarValue("origin"))],
            "Point");

        var converted = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(OpenTelemetrySerilogSink.ConvertValue(value));

        // Serilog.Sinks.OpenTelemetry omits the type tag by default.
        Assert.Equal([new KeyValuePair<string, object?>("X", 1), new KeyValuePair<string, object?>("Label", "origin")], converted);
    }

    [Fact]
    public void StructureValue_WithDuplicatePropertyNames_KeepsFirstProperty()
    {
        var value = new StructureValue([new LogEventProperty("X", new ScalarValue(1)), new LogEventProperty("X", new ScalarValue(2))]);

        var converted = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(OpenTelemetrySerilogSink.ConvertValue(value));

        Assert.Equal([new KeyValuePair<string, object?>("X", 1)], converted);
    }

    [Fact]
    public void DictionaryValue_IsConvertedToKeyValuePairs()
    {
        var value = new DictionaryValue(
        [
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(1), new ScalarValue("one")),
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("b"), new ScalarValue(true)),
            new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(null), new ScalarValue(2.5))
        ]);

        var converted = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(OpenTelemetrySerilogSink.ConvertValue(value));

        Assert.Equal(
            [new KeyValuePair<string, object?>("1", "one"), new KeyValuePair<string, object?>("b", true), new KeyValuePair<string, object?>("null", 2.5)],
            converted);
    }

    [Fact]
    public void SequenceValue_IsConvertedToArray()
    {
        var value = new SequenceValue([new ScalarValue(1), new ScalarValue("two"), new ScalarValue(true)]);

        var converted = Assert.IsAssignableFrom<Array>(OpenTelemetrySerilogSink.ConvertValue(value));

        Assert.Equal(new object?[] { 1, "two", true }, converted.Cast<object?>());
    }

    [Fact]
    public void NestedValues_AreConvertedRecursively()
    {
        var value = new StructureValue(
        [
            new LogEventProperty("Inner", new StructureValue([new LogEventProperty("Y", new ScalarValue(2))])),
            new LogEventProperty("Items", new SequenceValue([new ScalarValue(1), new ScalarValue(2)]))
        ]);

        var converted = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(OpenTelemetrySerilogSink.ConvertValue(value)).ToList();

        Assert.Equal(["Inner", "Items"], converted.Select(property => property.Key));
        var inner = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(converted[0].Value);
        Assert.Equal([new KeyValuePair<string, object?>("Y", 2)], inner);
        var items = Assert.IsAssignableFrom<Array>(converted[1].Value);
        Assert.Equal(new object?[] { 1, 2 }, items.Cast<object?>());
    }

    [Fact]
    public void StructureValue_IsRenderedAsSerilogText_WhenConvertedToString()
    {
        var inner = new StructureValue([new LogEventProperty("Label", new ScalarValue("origin"))]);
        var value = new StructureValue([new LogEventProperty("X", new ScalarValue(1)), new LogEventProperty("Inner", inner)], "Point");

        var converted = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(OpenTelemetrySerilogSink.ConvertValue(value));

        // The SDK writes a key/value list nested deeper than its recursion limit with Convert.ToString.
        Assert.Equal("Point { X: 1, Inner: { Label: \"origin\" } }", Convert.ToString(converted, CultureInfo.InvariantCulture));
        Assert.Equal("{ Label: \"origin\" }", Convert.ToString(converted.Single(property => property.Key == "Inner").Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void SequenceValue_WithStructureElements_IsConvertedToArrayOfElementsRenderedAsSerilogText()
    {
        var value = new SequenceValue(
        [
            new StructureValue([new LogEventProperty("X", new ScalarValue(1))]),
            new StructureValue([new LogEventProperty("X", new ScalarValue(2))], "Point")
        ]);

        var converted = Assert.IsAssignableFrom<Array>(OpenTelemetrySerilogSink.ConvertValue(value)).Cast<object?>().ToList();

        // The SDK writes a non-primitive array element with Convert.ToString.
        Assert.Equal([new KeyValuePair<string, object?>("X", 1)], Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(converted[0]));
        Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(converted[1]);
        Assert.Equal(["{ X: 1 }", "Point { X: 2 }"], converted.Select(element => Convert.ToString(element, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void SequenceValue_WithSequenceElements_IsConvertedToArrayOfElementsRenderedAsSerilogText()
    {
        var value = new SequenceValue(
        [
            new SequenceValue([new ScalarValue(1), new ScalarValue(2)]),
            new SequenceValue([new ScalarValue(3)])
        ]);

        var converted = Assert.IsAssignableFrom<Array>(OpenTelemetrySerilogSink.ConvertValue(value)).Cast<object?>();

        // The SDK writes a nested array element with Convert.ToString, which gives System.Object[] for a plain array.
        Assert.Equal(["[1, 2]", "[3]"], converted.Select(element => Convert.ToString(element, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void StructureValue_IsRenderedWithInvariantCulture_WhenConvertedToString()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var value = new StructureValue([new LogEventProperty("Ratio", new ScalarValue(1.5))]);

            var converted = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(OpenTelemetrySerilogSink.ConvertValue(value));

            Assert.Equal("{ Ratio: 1.5 }", Convert.ToString(converted, CultureInfo.InvariantCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void StructureValue_IsRenderedWithInvariantCulture_WhenToStringIsCalledWithoutFormatProvider()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var value = new StructureValue([new LogEventProperty("Ratio", new ScalarValue(1.5))]);

            var converted = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(OpenTelemetrySerilogSink.ConvertValue(value));

            Assert.Equal("{ Ratio: 1.5 }", converted.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void DictionaryValue_IsRenderedAsSerilogText_WhenConvertedToString()
    {
        var value = new DictionaryValue([new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("a"), new ScalarValue(1))]);

        var converted = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(OpenTelemetrySerilogSink.ConvertValue(value));

        Assert.Equal("[(\"a\": 1)]", Convert.ToString(converted, CultureInfo.InvariantCulture));
    }

    private static int GetOpenTelemetrySeverityValue(string val)
    {
        return (int)Enum.Parse(OpenTelemetryLogSeverityType, val);
    }
}
