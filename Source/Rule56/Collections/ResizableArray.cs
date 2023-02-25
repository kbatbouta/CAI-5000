using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace CombatAI
{
    public class ResizableArray<T> : IEnumerable<T>
    {
        private T[] array;

        public ResizableArray(int initialSize)
        {
            array = new T[initialSize];
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => array.Length;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => array[TransformIndex(index)];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (index < -Length || index >= Length)
                {
                    throw new IndexOutOfRangeException();
                }
                array[TransformIndex(index)] = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ResizableArrayEnum(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Expand(int targetLength)
        {
            if (targetLength < Length)
            {
                T[] expanded = new T[targetLength];
                Array.Copy(array, 0, expanded, 0, array.Length);
                array = expanded;
            }
        }

        private int TransformIndex(int index)
        {
            return index >= 0 ?
                index :
                Length - index;
        }

        private class ResizableArrayEnum : IEnumerator<T>
        {

            private readonly ResizableArray<T> _array;
            private          int               position = -1;

            public ResizableArrayEnum(ResizableArray<T> resizableArray)
            {
                _array = resizableArray;
            }

            public object Current
            {
                get
                {
                    try
                    {
                        return _array.array[position];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            T IEnumerator<T>.Current
            {
                get => (T)Current;
            }

            public bool MoveNext()
            {
                position++;
                return position < _array.Length;
            }

            public void Reset()
            {
                position = -1;
            }

            public void Dispose()
            {
            }
        }
    }
}
