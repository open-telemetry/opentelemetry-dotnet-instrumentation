// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace OpenTelemetry;

/// <summary>
/// Lock-free implementation of single-reader multi-writer circular buffer.
/// </summary>
/// <typeparam name="T">The type of the underlying value.</typeparam>
internal sealed class CircularBuffer<T>
    where T : class
{
    private readonly T?[] _Trait;
    private long _Head;
    private long _Tail;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
    /// </summary>
    /// <param name="capacity">The capacity of the circular buffer, must be a positive integer.</param>
    public CircularBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        Capacity = capacity;
        _Trait = new T[capacity];
    }

    /// <summary>
    /// Gets the capacity of the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the number of items contained in the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    public int Count
    {
        get
        {
            long tailSnapshot = Volatile.Read(ref _Tail);
            return (int)(Volatile.Read(ref _Head) - tailSnapshot);
        }
    }

    /// <summary>
    /// Gets the number of items added to the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    public long AddedCount => Volatile.Read(ref _Head);

    /// <summary>
    /// Gets the number of items removed from the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    public long RemovedCount => Volatile.Read(ref _Tail);

    /// <summary>
    /// Adds the specified item to the buffer.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>
    /// Returns <c>true</c> if the item was added to the buffer successfully;
    /// <c>false</c> if the buffer is full.
    /// </returns>
    public bool Add(T value)
    {
        Debug.Assert(value != null, "value was null");

        while (true)
        {
            long tailSnapshot = Volatile.Read(ref _Tail);
            long headSnapshot = Volatile.Read(ref _Head);

            if (headSnapshot - tailSnapshot >= Capacity)
            {
                return false; // buffer is full
            }

            if (Interlocked.CompareExchange(ref _Head, headSnapshot + 1, headSnapshot) != headSnapshot)
            {
                continue;
            }

            Volatile.Write(ref _Trait[headSnapshot % Capacity], value);

            return true;
        }
    }

    /// <summary>
    /// Attempts to add the specified item to the buffer.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <param name="maxSpinCount">The maximum allowed spin count, when set to a negative number or zero, will spin indefinitely.</param>
    /// <returns>
    /// Returns <c>true</c> if the item was added to the buffer successfully;
    /// <c>false</c> if the buffer is full or the spin count exceeded <paramref name="maxSpinCount"/>.
    /// </returns>
    public bool TryAdd(T value, int maxSpinCount)
    {
        if (maxSpinCount <= 0)
        {
            return Add(value);
        }

        Debug.Assert(value != null, "value was null");

        int spinCountDown = maxSpinCount;

        while (true)
        {
            long tailSnapshot = Volatile.Read(ref _Tail);
            long headSnapshot = Volatile.Read(ref _Head);

            if (headSnapshot - tailSnapshot >= Capacity)
            {
                return false; // buffer is full
            }

            if (Interlocked.CompareExchange(ref _Head, headSnapshot + 1, headSnapshot) != headSnapshot)
            {
                if (spinCountDown-- == 0)
                {
                    return false; // exceeded maximum spin count
                }

                continue;
            }

            Volatile.Write(ref _Trait[headSnapshot % Capacity], value);

            return true;
        }
    }

    /// <summary>
    /// Reads an item from the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    /// <remarks>
    /// This function is not reentrant-safe, only one reader is allowed at any given time.
    /// Warning: There is no bounds check in this method. Do not call unless you have verified Count > 0.
    /// </remarks>
    /// <returns>Item read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read()
    {
        long tail = Volatile.Read(ref _Tail);
        int index = (int)(tail % Capacity);
        while (true)
        {
            T? previous = Interlocked.Exchange(ref _Trait[index], null);
            if (previous == null)
            {
                // If we got here it means a writer isn't done.
                continue;
            }

            Volatile.Write(ref _Tail, tail + 1);
            return previous;
        }
    }
}
