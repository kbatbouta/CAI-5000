using System;
using Verse;

namespace CombatAI
{
    public class MapComponent_CombatAI : MapComponent
    {
        public CellFlooder flooder;

        public MapComponent_CombatAI(Map map) : base(map)
        {
            flooder = new CellFlooder(map);
        }
    }
}

