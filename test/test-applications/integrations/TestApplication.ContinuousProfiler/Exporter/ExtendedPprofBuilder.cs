// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
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

    public ExtendedPprofBuilder(string sampleType, string sampleUnit, string? periodType, string? periodUnit, long? period, long timestampNanoseconds)
    {
        var profileByteId = new byte[16];
        ActivityTraceId.CreateRandom().CopyTo(profileByteId);
        Profile = new Profile
        {
            ProfileId = ByteString.CopyFrom(profileByteId),
            TimeNanos = timestampNanoseconds,
        };

        var stringCache = new StringCache(Profile);

        Profile.SampleType.Add(new ValueType
        {
            TypeStrindex = stringCache.GetOrAdd(sampleType),
            UnitStrindex = stringCache.GetOrAdd(sampleUnit)
        });

        if (periodType != null && periodUnit != null)
        {
            Profile.PeriodType = new ValueType
            {
                TypeStrindex = stringCache.GetOrAdd(periodType),
                UnitStrindex = stringCache.GetOrAdd(periodUnit)
            };
        }

        if (period.HasValue)
        {
            Profile.Period = period.Value;
        }

        var functionCache = new FunctionCache(Profile, stringCache);
        _linkCache = new LinkCache(Profile);
        _attributeCache = new AttributeCache(Profile);
        var profileFrameTypeAttributeId = _attributeCache.GetOrAdd("profile.frame.type", value => value.StringValue = "dotnet");

        _locationCache = new LocationCache(Profile, functionCache, profileFrameTypeAttributeId);
    }

    public Profile Profile { get; }

    public int AddLocationId(string function) => _locationCache.Add(function);

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

    private class StringCache
    {
        private readonly Profile _profile;
        private readonly Dictionary<string, int> _table = new();
        private int _index;

        public StringCache(Profile profile)
        {
            _profile = profile;
            GetOrAdd(string.Empty); // 0 is reserved for the empty string
        }

        public int GetOrAdd(string str)
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
        private readonly Dictionary<string, int> _table = new();
        private int _index;

        public FunctionCache(Profile profile, StringCache stringCache)
        {
            _profile = profile;
            _stringCache = stringCache;
        }

        public int GetOrAdd(string functionName)
        {
            if (_table.TryGetValue(functionName, out var value))
            {
                return value;
            }

            // TODO There is a plan to make SystemName, FileName and StartLine number optional, as all of them are not always available.
            // TODO keeping a note here to double-check before going to production
            var function = new Function
            {
                // SystemNameStrindex = _stringCache.GetOrAdd("TODO How to handle SystemName in .NET?"),
                // FilenameStrindex = _stringCache.GetOrAdd("unknown"),
                // StartLine = 0,
                NameStrindex = _stringCache.GetOrAdd(functionName),
            }; // for now, we don't support file name

            _profile.FunctionTable.Add(function);
            _table[functionName] = _index;
            return _index++;
        }
    }

    private class LocationCache
    {
        private readonly Profile _profile;
        private readonly FunctionCache _functionCache;
        private readonly int _profileFrameTypeAttributeId;
        private int _index;

        public LocationCache(Profile profile, FunctionCache functionCache, int profileFrameTypeAttributeId)
        {
            _profile = profile;
            _functionCache = functionCache;
            _profileFrameTypeAttributeId = profileFrameTypeAttributeId;
        }

        public int Add(string function)
        {
            var location = new Location();
            location.AttributeIndices.Add(_profileFrameTypeAttributeId);
            location.Line.Add(new Line { FunctionIndex = _functionCache.GetOrAdd(function), Line_ = 0, Column = 0 }); // for now, we don't support line nor column number

            _profile.LocationTable.Add(location);

            return _index++;
        }
    }

    private class LinkCache
    {
        private readonly Profile _profile;
        private readonly Dictionary<Tuple<long, long, long>, int> _table = new();
        private int _index;

        public LinkCache(Profile profile)
        {
            _profile = profile;
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

            _profile.LinkTable.Add(link);
            _table[key] = _index;

            return _index++;
        }
    }

    private class AttributeCache
    {
        private readonly Profile _profile;
        private readonly Dictionary<KeyValue, int> _table = new();
        private int _index;

        public AttributeCache(Profile profile)
        {
            _profile = profile;
        }

        public int GetOrAdd(string name, Action<AnyValue> setValue)
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
