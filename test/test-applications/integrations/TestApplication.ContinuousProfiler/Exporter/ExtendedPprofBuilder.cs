// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Google.Protobuf;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Profiles.V1Development;
using ValueType = OpenTelemetry.Proto.Profiles.V1Development.ValueType;

namespace TestApplication.ContinuousProfiler;

internal sealed class ExtendedPprofBuilder
{
    private readonly LocationCache _locationCache;
    private readonly LinkCache _linkCache;
    private readonly AttributeCache _attributeCache;
    private readonly StringCache _stringCache;

    public ExtendedPprofBuilder(string sampleType, string sampleUnit, string? periodType, string? periodUnit, long? period, long timestampNanoseconds)
    {
        Dictionary = new ProfilesDictionary();

        // All dictionary tables must have a zero-value entry at index 0
        Dictionary.MappingTable.Add(new Mapping());
        Dictionary.LocationTable.Add(new Location());
        Dictionary.FunctionTable.Add(new Function());
        Dictionary.LinkTable.Add(new Link());
        Dictionary.StringTable.Add(string.Empty);
        Dictionary.AttributeTable.Add(new KeyValueAndUnit());
        Dictionary.StackTable.Add(new Stack());

        Profile = new Profile
        {
            TimeUnixNano = (ulong)timestampNanoseconds,
        };

        _stringCache = new StringCache(Dictionary);

        Profile.SampleType = new ValueType
        {
            TypeStrindex = _stringCache.GetOrAdd(sampleType),
            UnitStrindex = _stringCache.GetOrAdd(sampleUnit)
        };

        if (periodType != null && periodUnit != null)
        {
            Profile.PeriodType = new ValueType
            {
                TypeStrindex = _stringCache.GetOrAdd(periodType),
                UnitStrindex = _stringCache.GetOrAdd(periodUnit)
            };
        }

        if (period.HasValue)
        {
            Profile.Period = period.Value;
        }

        var functionCache = new FunctionCache(Dictionary, _stringCache);
        _linkCache = new LinkCache(Dictionary);
        _attributeCache = new AttributeCache(Dictionary, _stringCache);
        var profileFrameTypeAttributeId = _attributeCache.GetOrAdd("profile.frame.type", value => value.StringValue = "dotnet");

        _locationCache = new LocationCache(Dictionary, functionCache, profileFrameTypeAttributeId);
    }

    public Profile Profile { get; }

    public ProfilesDictionary Dictionary { get; }

    public int AddStack(IList<string> frames)
    {
        var stack = new Stack();

        for (var i = 0; i < frames.Count; i++)
        {
            stack.LocationIndices.Add(_locationCache.GetOrAdd(frames[i]));
        }

        Dictionary.StackTable.Add(stack);
        return Dictionary.StackTable.Count - 1;
    }

    public void AddLink(SampleBuilder sampleBuilder, long spanId, long traceIdHigh, long traceIdLow)
    {
        var linkId = _linkCache.GetOrAdd(spanId, traceIdHigh, traceIdLow);
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

    private sealed class StringCache
    {
        private readonly ProfilesDictionary _dictionary;
        private readonly Dictionary<string, int> _table = [];
        private int _index;

        public StringCache(ProfilesDictionary dictionary)
        {
            _dictionary = dictionary;
            // Index 0 is already the empty string added during dictionary initialization
            _table[string.Empty] = 0;
            _index = 1;
        }

        public int GetOrAdd(string str)
        {
            if (_table.TryGetValue(str, out var value))
            {
                return value;
            }

            _dictionary.StringTable.Add(str);
            _table[str] = _index;
            return _index++;
        }
    }

    private sealed class FunctionCache
    {
        private readonly ProfilesDictionary _dictionary;
        private readonly StringCache _stringCache;
        private readonly Dictionary<string, int> _table = [];
        private int _index;

        public FunctionCache(ProfilesDictionary dictionary, StringCache stringCache)
        {
            _dictionary = dictionary;
            _stringCache = stringCache;
            // Index 0 is the zero-value entry added during dictionary initialization
            _index = 1;
        }

        public int GetOrAdd(string functionName)
        {
            if (_table.TryGetValue(functionName, out var value))
            {
                return value;
            }

            var function = new Function
            {
                NameStrindex = _stringCache.GetOrAdd(functionName),
            };

            _dictionary.FunctionTable.Add(function);
            _table[functionName] = _index;
            return _index++;
        }
    }

    private sealed class LocationCache
    {
        private readonly ProfilesDictionary _dictionary;
        private readonly FunctionCache _functionCache;
        private readonly int _profileFrameTypeAttributeId;
        private readonly Dictionary<string, int> _table = [];
        private int _index;

        public LocationCache(ProfilesDictionary dictionary, FunctionCache functionCache, int profileFrameTypeAttributeId)
        {
            _dictionary = dictionary;
            _functionCache = functionCache;
            _profileFrameTypeAttributeId = profileFrameTypeAttributeId;
            // Index 0 is the zero-value entry added during dictionary initialization
            _index = 1;
        }

        public int GetOrAdd(string function)
        {
            if (_table.TryGetValue(function, out var value))
            {
                return value;
            }

            var location = new Location();
            location.AttributeIndices.Add(_profileFrameTypeAttributeId);
            location.Lines.Add(new Line { FunctionIndex = _functionCache.GetOrAdd(function), Line_ = 0, Column = 0 });

            _dictionary.LocationTable.Add(location);
            _table[function] = _index;

            return _index++;
        }
    }

    private sealed class LinkCache
    {
        private readonly ProfilesDictionary _dictionary;
        private readonly Dictionary<Tuple<long, long, long>, int> _table = [];
        private int _index;

        public LinkCache(ProfilesDictionary dictionary)
        {
            _dictionary = dictionary;
            // Index 0 is the zero-value entry added during dictionary initialization
            _index = 1;
        }

        public int GetOrAdd(long spanId, long traceIdHigh, long traceIdLow)
        {
            var key = Tuple.Create(spanId, traceIdHigh, traceIdLow);

            if (_table.TryGetValue(key, out var value))
            {
                return value;
            }

            var traceIdHighBytes = BitConverter.GetBytes(traceIdHigh);
            var traceIdLowBytes = BitConverter.GetBytes(traceIdLow);

            var traceIdBytes = new byte[traceIdHighBytes.Length + traceIdLowBytes.Length];

            traceIdHighBytes.CopyTo(traceIdBytes, 0);
            traceIdLowBytes.CopyTo(traceIdBytes, traceIdHighBytes.Length);

            var link = new Link
            {
                SpanId = UnsafeByteOperations.UnsafeWrap(BitConverter.GetBytes(spanId)),
                TraceId = UnsafeByteOperations.UnsafeWrap(traceIdBytes)
            };

            _dictionary.LinkTable.Add(link);
            _table[key] = _index;

            return _index++;
        }
    }

    private sealed class AttributeCache
    {
        private readonly ProfilesDictionary _dictionary;
        private readonly StringCache _stringCache;
        private readonly Dictionary<(string Name, string ValueStr), int> _table = [];
        private int _index;

        public AttributeCache(ProfilesDictionary dictionary, StringCache stringCache)
        {
            _dictionary = dictionary;
            _stringCache = stringCache;
            // Index 0 is the zero-value entry added during dictionary initialization
            _index = 1;
        }

        public int GetOrAdd(string name, Action<AnyValue> setValue)
        {
            var anyValue = new AnyValue();
            setValue(anyValue);

            var cacheKey = (name, anyValue.ToString());

            if (_table.TryGetValue(cacheKey, out var index))
            {
                return index;
            }

            var entry = new KeyValueAndUnit
            {
                KeyStrindex = _stringCache.GetOrAdd(name),
                Value = anyValue
            };

            _dictionary.AttributeTable.Add(entry);

            _table[cacheKey] = _index;

            return _index++;
        }
    }
}
