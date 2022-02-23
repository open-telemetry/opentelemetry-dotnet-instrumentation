using System;

namespace IntegrationTests.Helpers.Models;

public class EventArgs<T> : EventArgs
{
    public EventArgs(T value)
    {
        Value = value;
    }

    public T Value { get; }
}
