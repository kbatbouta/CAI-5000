using System;
using System.Runtime.CompilerServices;
namespace CombatAI
{
    /// <summary>
    ///     A faster implementation of a Queue based on arrays.
    /// </summary>
    public class FastHeap<T> where T : IComparable<T>
    {
        private int end;
        private T[] nodes;
        private int start;

        private FastHeap() { }
        /// <summary>
        ///     Constructor for FastQueue.
        /// </summary>
        /// <param name="initialSize">The starting size of the internal array.</param>
        public FastHeap(int initialSize = 64)
        {
            nodes = new T[initialSize];
        }

        /// <summary>
        ///     Wether the queue is empty
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Count == 0;
        }

        /// <summary>
        ///     Number of elements queued.
        /// </summary>
        public int Count
        {
            get;
            private set;
        }

        private T this[int index]
        {
            get => nodes[(index + start) % nodes.Length];
            set => nodes[(index + start) % nodes.Length] = value;
        }

        /// <summary>
        ///     Enqueue new elements into the queue.
        /// </summary>
        /// <param name="element"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T element)
        {
            T   node = nodes[end] = element, parentNode;
            int k    = Count;
            while (k > 0)
            {
                int parentIndex = (k - 1) / 2;
                if ((parentNode = this[parentIndex]).CompareTo(node) < 0)
                {
                    this[k]           = parentNode;
                    this[parentIndex] = node;
                    k                 = parentIndex;
                    continue;
                }
                break;
            }
            end++;
            Count++;
            if (end == start)
            {
                Expand();
            }
            if (end >= nodes.Length)
            {
                end = 0;
            }
        }

        /// <summary>
        ///     Dequeue new elements from the queue.
        /// </summary>
        /// <param name="element"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            Count--;
            T val = nodes[start++];
            if (Count > 0)
            {
                T   curNode = this[0];
                int k       = 0;
                while (true)
                {
                    int parentIndex = k;
                    int first       = 2 * k + 1;
                    int second      = first + 1;
                    T   tempNode;
                    if (first < Count && (tempNode = this[first]).CompareTo(curNode) > 0)
                    {
                        k       = first;
                        curNode = tempNode;
                    }
                    if (second < Count && (tempNode = this[second]).CompareTo(curNode) > 0)
                    {
                        k       = second;
                        curNode = tempNode;
                    }
                    if (k != parentIndex)
                    {
                        this[k]           = this[parentIndex];
                        this[parentIndex] = curNode;
                        continue;
                    }
                    break;
                }
            }
            if (start >= nodes.Length)
            {
                start = 0;
            }
            return val;
        }

        /// <summary>
        ///     Clear all elements in the queue.
        /// </summary>
        public void Clear()
        {
            start = 0;
            end   = 0;
            Count = 0;
        }

        /// <summary>
        ///     Will expand the node array so more items can be added
        /// </summary>
        private void Expand()
        {
            T[] temp = new T[nodes.Length * 4];
            Array.Copy(nodes, start, temp, start, nodes.Length - start);
            Array.Copy(nodes, 0, temp, nodes.Length, end);
            end   = start + nodes.Length;
            nodes = temp;
        }
    }
}
