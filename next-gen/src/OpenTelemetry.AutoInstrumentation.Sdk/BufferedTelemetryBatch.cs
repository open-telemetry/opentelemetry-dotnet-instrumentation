// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

using OpenTelemetry.Resources;

namespace OpenTelemetry;

internal sealed class BufferedTelemetryBatch<T>
    where T : class, IBufferedTelemetry<T>
{
    private readonly Dictionary<string, LinkedList> _Items = new(StringComparer.OrdinalIgnoreCase);
    private readonly Resource _Resource;

    public BufferedTelemetryBatch(
        Resource resource)
    {
        _Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }

    public void Add(T item)
    {
        Debug.Assert(item != null);

        ref LinkedList? linkedList = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _Items, item.Scope.Name, out bool exists);

        if (!exists)
        {
            linkedList = new();
        }

        T? tail = linkedList!.Tail;

        if (tail == null)
        {
            linkedList.Head = linkedList.Tail = item;
        }
        else
        {
            linkedList.Tail = tail.Next = item;
        }
    }

    public bool WriteTo<TBatchWriter>(
        TBatchWriter writer,
        Action<TBatchWriter, T> writeItemAction)
        where TBatchWriter : IBatchWriter
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.BeginBatch(_Resource);

        foreach (KeyValuePair<string, LinkedList> scope in _Items)
        {
            LinkedList linkedList = scope.Value;

            T? item = linkedList.Head;
            if (item == null)
            {
                continue;
            }

            writer.BeginInstrumentationScope(item.Scope);

            do
            {
                writeItemAction(writer, item);
            }
            while ((item = item.Next!) != null);

            writer.EndInstrumentationScope();
        }

        writer.EndBatch();

        return true;
    }

    public void Reset()
    {
        foreach (KeyValuePair<string, LinkedList> scope in _Items)
        {
            LinkedList linkedList = scope.Value;

            if (linkedList.Head != null)
            {
                linkedList.Head = linkedList.Tail = null;
            }
        }
    }

    private sealed class LinkedList
    {
        public T? Head;

        public T? Tail;
    }
}
