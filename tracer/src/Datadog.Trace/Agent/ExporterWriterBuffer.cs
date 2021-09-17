using System;

namespace Datadog.Trace.Agent
{
    internal class ExporterWriterBuffer<T>
    {
        private readonly T[] _items;

        private int _count;

        public ExporterWriterBuffer(int maxSize)
        {
            _items = new T[maxSize];
        }

        internal int MaxSize => _items.Length;

        public bool Push(T item)
        {
            lock (_items)
            {
                if (_count >= _items.Length)
                {
                    // drop the trace as the buffer is full
                    return false;
                }

                _items[_count++] = item;
                return true;
            }
        }

        public T[] Pop()
        {
            lock (_items)
            {
                // copy items from buffer into new array
                var result = new T[_count];
                Array.Copy(_items, result, _count);

                // clear buffer
                Array.Clear(_items, 0, _items.Length);
                _count = 0;

                return result;
            }
        }
    }
}
