// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Google.Protobuf;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Profiles.V1.Alternatives.PprofExtended;

namespace TestApplication.ContinuousProfiler;

internal class ExtendedPprofBuilder
{
    private readonly LocationTable _locationTable;
    private readonly LinkTable _linkTable;
    private readonly AttributeCache _attributeCache;

    public ExtendedPprofBuilder()
    {
        Profile = new Profile();
        var stringCache = new StringCache(Profile);
        var functionCache = new FunctionCache(Profile, stringCache);
        _locationTable = new LocationTable(Profile, functionCache);
        _linkTable = new LinkTable(Profile);
        _attributeCache = new AttributeCache(Profile);
    }

    public Profile Profile { get; }

    public ulong GetLocationId(string function) => _locationTable.Get(function);

    public void AddLink(SampleBuilder sampleBuilder, long spanId, long traceIdHigh, long traceIdLow)
    {
        var linkId = _linkTable.Get(spanId, traceIdHigh, traceIdLow);
        sampleBuilder.SetLink(linkId);
    }

    public void AddAttribute(SampleBuilder sample, string name, string value)
    {
        AddAttribute(sample, name, anyValue => anyValue.StringValue = value);
    }

    public void AddAttribute(SampleBuilder sampleBuilder, string name, long value)
    {
        AddAttribute(sampleBuilder, name, anyValue => anyValue.IntValue = value);
    }

    private void AddAttribute(SampleBuilder sampleBuilder, string name, Action<AnyValue> setValue)
    {
        var attributeId = _attributeCache.GetOrAdd(name, setValue);

        sampleBuilder.AddAttribute(attributeId);
    }

    private class StringCache
    {
        private readonly Profile _profile;
        private readonly Dictionary<string, long> _table = new();
        private long _index;

        public StringCache(Profile profile)
        {
            _profile = profile;
            GetOrAdd(string.Empty); // 0 is reserved for the empty string
        }

        public long GetOrAdd(string str)
        {
            if (_table.TryGetValue(str, out var value))
            {
                return value;
            }

            _profile.StringTable.Add(str);
            _table[str] = _index;
            return _index++;
        }
    }

    private class FunctionCache
    {
        private readonly Profile _profile;
        private readonly StringCache _stringCache;
        private readonly Dictionary<string, ulong> _table = new();
        private ulong _index = 1; // 0 is reserved

        public FunctionCache(Profile profile, StringCache stringCache)
        {
            _profile = profile;
            _stringCache = stringCache;
        }

        public ulong GetOrAdd(string functionName)
        {
            if (_table.TryGetValue(functionName, out var value))
            {
                return value;
            }

            var function = new Function { Id = _index, Filename = _stringCache.GetOrAdd("unknown"), Name = _stringCache.GetOrAdd(functionName) }; // for now, we don't support file name

            _profile.Function.Add(function);
            _table[functionName] = _index;
            return _index++;
        }
    }

    private class LocationTable
    {
        private readonly Profile _profile;
        private readonly FunctionCache _functionCache;
        private ulong _index = 1; // 0 is reserved

        public LocationTable(Profile profile, FunctionCache functionCache)
        {
            _profile = profile;
            _functionCache = functionCache;
        }

        public ulong Get(string function)
        {
            var functionKey = function;

            var location = new Location { Id = _index };
            location.Line.Add(new Line { FunctionIndex = _functionCache.GetOrAdd(functionKey), Line_ = 0, Column = 0 }); // for now, we don't support line nor column number

            _profile.Location.Add(location);
            return _index++;
        }
    }

    private class LinkTable
    {
        private readonly Profile _profile;
        private ulong _index = 1; // 0 is reserved

        public LinkTable(Profile profile)
        {
            _profile = profile;
        }

        public ulong Get(long spanId, long traceIdHigh, long traceIdLow)
        {
            var traceIdHighBytes = BitConverter.GetBytes(traceIdHigh);
            var traceIdLowBytes = BitConverter.GetBytes(traceIdLow);

            var link = new Link
            {
                SpanId = UnsafeByteOperations.UnsafeWrap(BitConverter.GetBytes(spanId)),
                TraceId = UnsafeByteOperations.UnsafeWrap(traceIdHighBytes.Concat(traceIdLowBytes).ToArray())
            };

            _profile.LinkTable.Add(link);

            return _index++;
        }
    }

    private class AttributeCache
    {
        private readonly Profile _profile;
        private readonly Dictionary<KeyValue, ulong> _table = new();
        private ulong _index = 1; // 0 is reserved

        public AttributeCache(Profile profile)
        {
            _profile = profile;
        }

        public ulong GetOrAdd(string name, Action<AnyValue> setValue)
        {
            var keyValue = new KeyValue
            {
                Key = name,
                Value = new AnyValue()
            };

            setValue(keyValue.Value);

            if (_table.TryGetValue(keyValue, out var index))
            {
                return index;
            }

            _table[keyValue] = _index;

            _profile.AttributeTable.Add(keyValue);

            return _index++;
        }
    }
}
