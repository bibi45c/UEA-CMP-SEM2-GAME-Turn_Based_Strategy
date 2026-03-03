using System;
using System.Collections.Generic;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Generic min-heap (priority queue) for A* pathfinding.
    /// Elements with the smallest priority value are dequeued first.
    /// </summary>
    public class MinHeap<T> where T : IComparable<T>
    {
        private readonly List<T> _data;

        public int Count => _data.Count;
        public bool IsEmpty => _data.Count == 0;

        public MinHeap(int initialCapacity = 16)
        {
            _data = new List<T>(initialCapacity);
        }

        public void Push(T item)
        {
            _data.Add(item);
            BubbleUp(_data.Count - 1);
        }

        public T Pop()
        {
            if (_data.Count == 0)
                throw new InvalidOperationException("MinHeap is empty.");

            T top = _data[0];
            int last = _data.Count - 1;
            _data[0] = _data[last];
            _data.RemoveAt(last);

            if (_data.Count > 0)
                BubbleDown(0);

            return top;
        }

        public T Peek()
        {
            if (_data.Count == 0)
                throw new InvalidOperationException("MinHeap is empty.");
            return _data[0];
        }

        public void Clear()
        {
            _data.Clear();
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_data[index].CompareTo(_data[parent]) >= 0)
                    break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void BubbleDown(int index)
        {
            int count = _data.Count;
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < count && _data[left].CompareTo(_data[smallest]) < 0)
                    smallest = left;
                if (right < count && _data[right].CompareTo(_data[smallest]) < 0)
                    smallest = right;

                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            T temp = _data[a];
            _data[a] = _data[b];
            _data[b] = temp;
        }
    }
}
