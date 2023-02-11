using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;
namespace CombatAI
{
    public class AIEnvThings : ICollection, IExposable
    {
        private const AIEnvAgentState      invalidState = (AIEnvAgentState)(-1);
        
        private       List<AIEnvAgentInfo> things;

        public AIEnvThings() : this(1)
        {
        }

        public AIEnvThings(int alloc)
        {
            things     = new List<AIEnvAgentInfo>(alloc);
            IsReadOnly = false;
            SyncRoot   = new object();
        }

        private AIEnvThings(List<AIEnvAgentInfo> things)
        {
            this.things = things;
        }

        public AIEnvThings AsReadonly
        {
            get
            {
                if (IsReadOnly)
                {
                    return this;
                }
                AIEnvThings copy = new AIEnvThings(things);
                copy.IsReadOnly = true;
                copy.SyncRoot   = SyncRoot;
                return copy;
            }
        }

        public bool IsReadOnly
        {
            get;
            private set;
        }

        public AIEnvAgentInfo this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => things[index];
        }

        public AIEnvAgentState this[Thing thing]
        {
            get
            {
                for (int i = 0; i < things.Count; i++)
                {
                    AIEnvAgentInfo temp = things[i];
                    if (temp.thing == thing)
                    {
                        return temp.state;
                    }
                }
                return AIEnvAgentState.unknown;
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new Exception("Collection is readonly");
                }
                for (int i = 0; i < things.Count; i++)
                {
                    AIEnvAgentInfo temp = things[i];
                    if (temp.thing == thing)
                    {
                        things[i] = new AIEnvAgentInfo(thing, value);
                        return;
                    }
                }
                things.Add(new AIEnvAgentInfo(thing, value));
            }
        }

        public int Count
        {
            get => things.Count;
        }

        public object SyncRoot
        {
            get;
            private set;
        }

        public bool IsSynchronized
        {
            get => false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            for (int i = 0; i < things.Count; i++)
            {
                array.SetValue(things[i], index + i);
            }
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                things.RemoveAll(t => t.thing == null || t.thing.Destroyed);
            }
//            Scribe_Collections.Look(ref things, "collectionThings", LookMode.Deep);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                things ??= new List<AIEnvAgentInfo>();
                things.RemoveAll(t => t.thing == null || t.thing.Destroyed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(AIEnvAgentInfo item)
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            this[item.thing] = item.state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Thing thing, AIEnvAgentState state)
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            this[thing] = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(AIEnvAgentInfo item)
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            return things.Remove(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(Thing thing)
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            return things.RemoveAll(i => i.thing == thing) > 0;
        }

        public void Clear()
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            things.Clear();
        }

        public void ClearAndAddRange(HashSet<AIEnvAgentInfo> things)
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            this.things.Clear();
            this.things.AddRange(things);
        }
        
        public void ClearAndAddRange(List<AIEnvAgentInfo> things)
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            this.ClearAndAddRange(things.ToHashSet());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(AIEnvAgentInfo item)
        {
            return Contains(item.thing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Thing item)
        {
            for (int i = 0; i < things.Count; i++)
            {
                if ( things[i].thing == item)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(AIEnvAgentInfo[] array, int arrayIndex)
        {
            things.CopyTo(array, arrayIndex);
        }

        public IEnumerator<AIEnvAgentInfo> GetEnumerator()
        {
            return new AIThingEnum(things, invalidState);
        }
        
        public IEnumerator<AIEnvAgentInfo> GetEnumerator(AIEnvAgentState state)
        {
            return new AIThingEnum(things, state);
        }

        public class AIThingEnum : IEnumerator<AIEnvAgentInfo>
        {
            public readonly List<AIEnvAgentInfo> _items;
            public readonly AIEnvAgentState      _state;
            
            private int index = -1;

            public AIThingEnum(List<AIEnvAgentInfo> items, AIEnvAgentState state)
            {
                _items = items;
                _state = state;
            }

            public void Dispose()
            {
            }
            
            public bool MoveNext()
            {
                if (_state == invalidState)
                {
                    return MoveNextInternal();
                }
                else
                {
                    while (MoveNextInternal())
                    {
                        if ((_items[index].state & _state) == _state)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            

            public void Reset()
            {
                index = -1;
            }

            public AIEnvAgentInfo Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _items[index];
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Current;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool MoveNextInternal()
            {
                index++;
                return index < _items.Count;
            }
        }
    }
}
