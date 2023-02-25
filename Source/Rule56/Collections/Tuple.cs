using System;
using System.Runtime.CompilerServices;
namespace CombatAI
{
    public struct Tuple<A1, A2> : IEquatable<Tuple<A1, A2>>
    {
        public A1 val1;
        public A2 val2;

        public Tuple(A1 val1, A2 val2)
        {
            this.val1 = val1;
            this.val2 = val2;
        }

        public Tuple(Tuple<A1, A2> other)
        {
            val1 = other.val1;
            val2 = other.val2;
        }

        public override bool Equals(object obj)
        {
            return obj is Tuple<A1, A2> other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Tuple<A1, A2> other)
        {
            return val1.Equals(other.val1) && val2.Equals(other.val2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int hash = 36469;
            unchecked
            {
                hash = hash * 17 + val1.GetHashCode();
                hash = hash * 17 + val2.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            return $"({val1.ToString()}, {val2.ToString()})";
        }
    }

    public struct Tuple<A1, A2, A3> : IEquatable<Tuple<A1, A2, A3>>
    {
        public A1 val1;
        public A2 val2;
        public A3 val3;

        public Tuple(A1 val1, A2 val2, A3 val3)
        {
            this.val1 = val1;
            this.val2 = val2;
            this.val3 = val3;
        }

        public Tuple(Tuple<A1, A2, A3> other)
        {
            val1 = other.val1;
            val2 = other.val2;
            val3 = other.val3;
        }

        public override bool Equals(object obj)
        {
            return obj is Tuple<A1, A2, A3> other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Tuple<A1, A2, A3> other)
        {
            return val1.Equals(other.val1) && val2.Equals(other.val2) && val3.Equals(other.val3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int hash = 36469;
            unchecked
            {
                hash = hash * 17 + val1.GetHashCode();
                hash = hash * 17 + val2.GetHashCode();
                hash = hash * 17 + val3.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            return $"({val1.ToString()}, {val2.ToString()}, {val3.ToString()})";
        }
    }

    public struct Tuple<A1, A2, A3, A4> : IEquatable<Tuple<A1, A2, A3, A4>>
    {
        public A1 val1;
        public A2 val2;
        public A3 val3;
        public A4 val4;

        public Tuple(A1 val1, A2 val2, A3 val3, A4 val4)
        {
            this.val1 = val1;
            this.val2 = val2;
            this.val3 = val3;
            this.val4 = val4;
        }

        public Tuple(Tuple<A1, A2, A3, A4> other)
        {
            val1 = other.val1;
            val2 = other.val2;
            val3 = other.val3;
            val4 = other.val4;
        }

        public override bool Equals(object obj)
        {
            return obj is Tuple<A1, A2, A3, A4> other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Tuple<A1, A2, A3, A4> other)
        {
            return val1.Equals(other.val1) && val2.Equals(other.val2) && val3.Equals(other.val3) && val4.Equals(other.val4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int hash = 36469;
            unchecked
            {
                hash = hash * 17 + val1.GetHashCode();
                hash = hash * 17 + val2.GetHashCode();
                hash = hash * 17 + val3.GetHashCode();
                hash = hash * 17 + val4.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            return $"({val1.ToString()}, {val2.ToString()}, {val3.ToString()}, {val4.ToString()})";
        }
    }

    public struct Tuple<A1, A2, A3, A4, A5> : IEquatable<Tuple<A1, A2, A3, A4, A5>>
    {
        public A1 val1;
        public A2 val2;
        public A3 val3;
        public A4 val4;
        public A5 val5;

        public Tuple(A1 val1, A2 val2, A3 val3, A4 val4, A5 val5)
        {
            this.val1 = val1;
            this.val2 = val2;
            this.val3 = val3;
            this.val4 = val4;
            this.val5 = val5;
        }

        public Tuple(Tuple<A1, A2, A3, A4, A5> other)
        {
            val1 = other.val1;
            val2 = other.val2;
            val3 = other.val3;
            val4 = other.val4;
            val5 = other.val5;
        }

        public override bool Equals(object obj)
        {
            return obj is Tuple<A1, A2, A3, A4, A5> other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Tuple<A1, A2, A3, A4, A5> other)
        {
            return val1.Equals(other.val1) && val2.Equals(other.val2) && val3.Equals(other.val3) && val4.Equals(other.val4) && val5.Equals(other.val5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int hash = 36469;
            unchecked
            {
                hash = hash * 17 + val1.GetHashCode();
                hash = hash * 17 + val2.GetHashCode();
                hash = hash * 17 + val3.GetHashCode();
                hash = hash * 17 + val4.GetHashCode();
                hash = hash * 17 + val5.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            return $"({val1.ToString()}, {val2.ToString()}, {val3.ToString()}, {val4.ToString()}, {val5.ToString()})";
        }
    }
}
