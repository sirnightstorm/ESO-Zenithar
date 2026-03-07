using System;
using System.Collections.Generic;
using System.Text;

namespace ZenitharClient.Src
{
    public class RollingStringBuffer
    {
        private readonly string[] _buffer;
        private int _nextIndex;
        private int _count;

        public event EventHandler? Changed;

        public int Capacity { get; }
        public int Count => _count;

        public RollingStringBuffer(int capacity = 100)
        {
            Capacity = capacity;
            _buffer = new string[capacity];
            _nextIndex = 0;
            _count = 0;
        }

        public void Add(string value)
        {
            _buffer[_nextIndex] = value;

            _nextIndex = (_nextIndex + 1) % Capacity;

            if (_count < Capacity)
                _count++;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<string> GetItems()
        {
            for (int i = 0; i < _count; i++)
            {
                int index = (_nextIndex - _count + i + Capacity) % Capacity;
                yield return _buffer[index];
            }
        }

        public override string ToString()
            => string.Join(", ", GetItems());
    }
}
