// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Proto.Profiles.V1.Alternatives.PprofExtended;

namespace TestApplication.ContinuousProfiler;

internal class ExtendedPprofBuilder
{
    private readonly StringTable _stringTable;
    private readonly LocationTable _locationTable;

    public ExtendedPprofBuilder()
    {
        Profile = new Profile();
        _stringTable = new StringTable(Profile);
        var functionTable = new FunctionTable(Profile, _stringTable);
        _locationTable = new LocationTable(Profile, functionTable);
    }

    public Profile Profile { get; }

    public long GetStringId(string str) => _stringTable.Get(str);

    public ulong GetLocationId(string function) => _locationTable.Get(function);

    public void AddLabel(SampleBuilder sample, string name, string value)
    {
        AddLabel(sample, name, label => label.Str = _stringTable.Get(value));
    }

    public void AddLabel(SampleBuilder sampleBuilder, string name, long value)
    {
        AddLabel(sampleBuilder, name, label => label.Num = value);
    }

    private void AddLabel(SampleBuilder sampleBuilder, string name, Action<Label> setLabel)
    {
        var label = new Label { Key = _stringTable.Get(name) };
        setLabel(label);
        sampleBuilder.AddLabel(label);
    }

    private class StringTable
    {
        private readonly Profile _profile;
        private readonly Dictionary<string, long> _table = new();
        private long _index;

        public StringTable(Profile profile)
        {
            _profile = profile;
            Get(string.Empty); // 0 is reserved for the empty string
        }

        public long Get(string str)
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

    private class FunctionTable
    {
        private readonly Profile _profile;
        private readonly StringTable _stringTable;
        private readonly Dictionary<string, ulong> _table = new();
        private ulong _index = 1; // 0 is reserved

        public FunctionTable(Profile profile, StringTable stringTable)
        {
            _profile = profile;
            _stringTable = stringTable;
        }

        public ulong Get(string functionName)
        {
            if (_table.TryGetValue(functionName, out var value))
            {
                return value;
            }

            var function = new Function { Id = _index, Filename = _stringTable.Get("unknown"), Name = _stringTable.Get(functionName) }; // for now, we don't support file name

            _profile.Function.Add(function);
            _table[functionName] = _index;
            return _index++;
        }
    }

    private class LocationTable
    {
        private readonly Profile _profile;
        private readonly FunctionTable _functionTable;
        private ulong _index = 1; // 0 is reserved

        public LocationTable(Profile profile, FunctionTable functionTable)
        {
            _profile = profile;
            _functionTable = functionTable;
        }

        public ulong Get(string function)
        {
            var functionKey = function;

            var location = new Location { Id = _index };
            location.Line.Add(new Line { FunctionIndex = _functionTable.Get(functionKey), Line_ = 0, Column = 0 }); // for now, we don't support line nor column number

            _profile.Location.Add(location);
            return _index++;
        }
    }
}
