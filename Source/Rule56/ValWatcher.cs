using System;
using Verse;
namespace CombatAI
{
    public class ValWatcher<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        private T value;
        private int changedTimestamp;
        private int minChangedInterval;
        private Action<T, T, int> onChangedAction;        

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
                    this.value = value;                    
                    this.changedTimestamp = GenTicks.TicksGame;                    
                }
            }
        }

        public int TicksSinceLastChanged
        {
            get => GenTicks.TicksGame - changedTimestamp;
        }

        public ValWatcher(T value, Action<T, T, int> onChangedAction, int minChangedInterval)
        {
            this.value = value;
            this.changedTimestamp = GenTicks.TicksGame;
            this.minChangedInterval = minChangedInterval;
            this.onChangedAction = onChangedAction;            
        }        

        public void ResetTimeStamp()
        {
            this.changedTimestamp = GenTicks.TicksGame;
        }

        public void SetTimeStamp(int ticks)
        {
            this.changedTimestamp = ticks;
        }

        public override string ToString()
        {
            return $"ValWatcher<{value},{TicksSinceLastChanged}>";
        }
    }
}

