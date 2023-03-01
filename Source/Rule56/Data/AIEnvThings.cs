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
        private const    AIEnvAgentState      invalidState = AIEnvAgentState.unknown;
        private readonly List<AIEnvAgentInfo> elements     = new List<AIEnvAgentInfo>();

        private readonly Dictionary<int, AIEnvAgentInfo> stateByThing = new Dictionary<int, AIEnvAgentInfo>();

        public AIEnvThings()
        {
            IsReadOnly = false;
            SyncRoot   = new object();
        }

        private AIEnvThings(List<AIEnvAgentInfo> elements)
        {
            this.elements = elements;
        }

        public AIEnvAgentInfo Random
        {
            get => elements[Rand.Int % elements.Count];
        }

        public AIEnvThings AsReadonly
        {
            get
            {
                if (IsReadOnly)
                {
                    return this;
                }
                AIEnvThings copy = new AIEnvThings(elements);
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
            get => elements[index];
        }

        public AIEnvAgentInfo this[Thing thing]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => stateByThing.TryGetValue(thing.thingIDNumber, out AIEnvAgentInfo store) ? store : new AIEnvAgentInfo(null, AIEnvAgentState.unknown);
            private set
            {
                if (IsReadOnly)
                {
                    throw new Exception("Collection is readonly");
                }
                if (thing == null)
                {
                    return;
                }
                if (stateByThing.ContainsKey(thing.thingIDNumber))
                {
                    for (int i = 0; i < elements.Count; i++)
                    {
                        AIEnvAgentInfo temp = elements[i];
                        if (temp.thing == thing)
                        {
                            elements[i]                       = value;
                            stateByThing[thing.thingIDNumber] = value;
                            return;
                        }
                    }
                    throw new Exception("AIEnvThings stateByThing contains key but the key is missing from things.");
                }
                elements.Add(value);
                stateByThing[thing.thingIDNumber] = value;
            }
        }

        public int Count
        {
            get => elements.Count;
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
            for (int i = 0; i < elements.Count; i++)
            {
                array.SetValue(elements[i], index + i);
            }
        }

        public void ExposeData()
        {
        }

        public void Clear()
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            elements.Clear();
            stateByThing.Clear();
        }

        public void ClearAndAddRange(HashSet<AIEnvAgentInfo> items)
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            elements.Clear();
            elements.AddRange(items);
            // update ids.
            stateByThing.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                stateByThing[elements[i].thing.thingIDNumber] = elements[i];
            }
        }
        public void ClearAndAddRange(Dictionary<Thing, AIEnvAgentInfo> dict)
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            elements.Clear();
            stateByThing.Clear();
            foreach (KeyValuePair<Thing, AIEnvAgentInfo> pair in dict)
            {
                if (pair.Key != pair.Value.thing)
                {
                    throw new InvalidOperationException("Key must match the value of AIEnvAgentInfo.Thing");
                }
                elements.Add(pair.Value);
                stateByThing[pair.Key.thingIDNumber] = pair.Value;
            }
        }

        public void ClearAndAddRange(List<AIEnvAgentInfo> items)
        {
            if (IsReadOnly)
            {
                throw new Exception("Collection is readonly");
            }
            ClearAndAddRange(items.ToHashSet());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(AIEnvAgentInfo item)
        {
            return Contains(item.thing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Thing item)
        {
            return stateByThing.ContainsKey(item.thingIDNumber);
        }

        public void CopyTo(AIEnvAgentInfo[] array, int arrayIndex)
        {
            elements.CopyTo(array, arrayIndex);
        }

        public IEnumerator<AIEnvAgentInfo> GetEnumerator()
        {
            return new AIThingEnum(elements, invalidState);
        }

        public IEnumerator<AIEnvAgentInfo> GetEnumerator(AIEnvAgentState state)
        {
            return new AIThingEnum(elements, state);
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
                while (MoveNextInternal())
                {
                    if ((_items[index].state & _state) == _state)
                    {
                        return true;
                    }
                }
                return false;
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
