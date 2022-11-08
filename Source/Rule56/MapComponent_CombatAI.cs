using System;
using Verse;

namespace CombatAI
{
    public class MapComponent_CombatAI : MapComponent
    {
        public CellFlooder flooder;
        public TGrid<float> tempGrid;

        public MapComponent_CombatAI(Map map) : base(map)
        {
            flooder = new CellFlooder(map);
            tempGrid = new TGrid<float>(map);
        }
    }
}

