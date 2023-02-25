using System;
using Verse;
namespace CombatAI
{
    public class ValWatcher<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        private readonly int               minChangedInterval;
        private readonly Action<T, T, int> onChangedAction;
        private          int               changedTimestamp;
        private          T                 value;

        public ValWatcher(T value, Action<T, T, int> onChangedAction, int minChangedInterval)
        {
            this.value              = value;
            changedTimestamp        = GenTicks.TicksGame;
            this.minChangedInterval = minChangedInterval;
            this.onChangedAction    = onChangedAction;
        }

        public T Current
        {
            get => value;
            set
            {
                if (!value.Equals(this.value) && TicksSinceLastChanged >= minChangedInterval)
                {
                    if (onChangedAction != null)
                    {
                        onChangedAction(value, this.value, TicksSinceLastChanged);
                    }
                    this.value       = value;
                    changedTimestamp = GenTicks.TicksGame;
                }
            }
        }

        public int TicksSinceLastChanged
        {
            get => GenTicks.TicksGame - changedTimestamp;
        }

        public void ResetTimeStamp()
        {
            changedTimestamp = GenTicks.TicksGame;
        }

        public void SetTimeStamp(int ticks)
        {
            changedTimestamp = ticks;
        }

        public override string ToString()
        {
            return $"ValWatcher<{value},{TicksSinceLastChanged}>";
        }
    }
}
