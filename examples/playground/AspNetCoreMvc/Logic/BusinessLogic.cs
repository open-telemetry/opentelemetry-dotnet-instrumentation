// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace Examples.AspNetCoreMvc.Logic;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// Provides business logic functionality for the application.
/// </summary>
public class BusinessLogic
{
    // No arguments
    public string ProcessBusinessOperation()
    {
        return "Default Operation";
    }

    // 1 argument - primitive
    public string ProcessBusinessOperation(Span<char> operationName)
    {
        return new string(operationName);
    }

    // 2 arguments - primitive and value type
    public string ProcessBusinessOperation(int operationId, DateTime timestamp)
    {
        return $"Operation {operationId} at {timestamp}";
    }

    // 3 arguments - primitive, reference type, and enum
    public string ProcessBusinessOperation(string operationName, Uri resourceUri, DayOfWeek scheduledDay)
    {
        return $"{operationName} for {resourceUri} on {scheduledDay}";
    }

    // 4 arguments - primitive, nullable, collection, and delegate
    public string ProcessBusinessOperation(double amount, int? priority, string[] tags, Func<string, bool> validator)
    {
        var priorityText = priority.HasValue ? priority.ToString() : "unspecified";
        return $"Amount: {amount}, Priority: {priorityText}, Tags: {string.Join(",", tags)}";
    }

    // 5 arguments - struct, generic, interface, tuple, and array
    public string ProcessBusinessOperation<T>(TimeSpan duration, List<T> items, IComparable comparable, (string Name, int Value) metadata, byte[] data)
    {
        return $"Duration: {duration}, Items: {items.Count}, Metadata: {metadata.Name}:{metadata.Value}, Data size: {data.Length}";
    }

    // 6 arguments - complex types and generics
    public string ProcessBusinessOperation<TKey, TValue>(
        Dictionary<TKey, TValue> mappings,
        Task<string> asyncResult,
        Lazy<object> lazyObject,
        string buffer,
        CancellationToken cancellationToken,
        IProgress<int> progress)
        where TKey : notnull
    {
        return $"Mappings: {mappings.Count}, Buffer size: {buffer.Length}";
    }

    // 7 arguments - mix of value and reference types
    public string ProcessBusinessOperation(
        Guid id,
        Type objectType,
        Stream dataStream,
        Exception error,
        KeyValuePair<string, object> property,
        Action<string> logger,
        HashSet<string> uniqueValues)
    {
        return $"Processing {id} of type {objectType.Name}, Unique values: {uniqueValues.Count}";
    }

    // 8 arguments - advanced types
    public string ProcessBusinessOperation(
        object instance,
        Delegate callback,
        WeakReference reference,
        Predicate<string> filter,
        ReadOnlyMemory<byte> memory,
        IEnumerable<KeyValuePair<string, string>> headers,
        IFormatProvider formatProvider,
        System.Reflection.MethodInfo method)
    {
        return $"Instance: {instance}, Method: {method.Name}, Headers: {headers.Count()}";
    }

    // 9 string arguments
    public string ProcessBusinessOperation(
        string operationName,
        string category,
        string description,
        string source,
        string target,
        string priority,
        string status,
        string metadata,
        string correlationId)
    {
        return $"{operationName} - Category: {category}, Description: {description}, Source: {source}, Target: {target}, Priority: {priority}, Status: {status}, Metadata: {metadata}, CorrelationId: {correlationId}";
    }
}
