using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace CombatAI
{
    public class AvoidanceTracker : MapComponent
    {
        public class AvoidanceReader
        {
            public IShortTermMemoryGrid danger;            
            public IShortTermMemoryGrid proximity;                     

            private readonly Map map;
            private readonly CellIndices indices;
            private readonly AvoidanceTracker tacker;

            public AvoidanceReader(AvoidanceTracker tracker)
            {
                this.tacker = tracker;
                this.map = tracker.map;
                this.indices = tracker.map.cellIndices;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetDanger(IntVec3 cell) => GetDanger(indices.CellToIndex(cell));
            public float GetDanger(int index) => danger != null ? Mathf.Min(danger[index], 36f) : 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetProximity(IntVec3 cell) => GetProximity(indices.CellToIndex(cell));
            public float GetProximity(int index) => proximity != null ? Mathf.Min(proximity[index] * 0.5f, 16)   : 0f;                      
        }

        public IShortTermMemoryHandler danger;        
        public IShortTermMemoryHandler[] proximity = new IShortTermMemoryHandler[2];        

        public AvoidanceTracker(Map map) : base(map)
        {
            danger = new IShortTermMemoryHandler(map);                       
            proximity[0] = new IShortTermMemoryHandler(map, 80, 16);           
            proximity[1] = new IShortTermMemoryHandler(map, 120, 16);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();            
            // tick danger grid
            danger.Tick();                        
            // update formation avoidence grid
            proximity[0].Tick();
            proximity[1].Tick();

            if ((Finder.Settings.Debug_DrawAvoidanceGrid_Danger || Finder.Settings.Debug_DrawAvoidanceGrid_Proximity) && GenTicks.TicksGame % 15 == 0)
            {
                IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 56, true))
                    {
                        if (cell.InBounds(map))
                        {
                            var value = 0f;
                            if (Finder.Settings.Debug_DrawAvoidanceGrid_Danger)
                            {
                                value += danger.grid[cell];
                            }
                            if (Finder.Settings.Debug_DrawAvoidanceGrid_Proximity)
                            {
                                value += proximity[0].grid[cell] + proximity[1].grid[cell];
                            }
                            if (value > 0f)
                            {
                                map.debugDrawer.FlashCell(cell, value / 40f, $"d{Math.Round(danger.grid[cell], 1)}_p{Math.Round(proximity[0].grid[cell] + proximity[1].grid[cell], 1)}", 15);
                            }                            
                        }
                    }
                }
            }
        }

        public bool TryGetReader(Pawn pawn, out AvoidanceReader reader)
        {
            reader = null;
            if (map.ParentFaction == null)
            {
                return false;
            }            
            reader = new AvoidanceReader(this);
            reader.danger = danger.grid;
            Faction faction = pawn.Faction;
            if (faction != null && !faction.IsPlayerSafe() && ((pawn.jobs?.curJob?.def?.alwaysShowWeapon ?? false) || (pawn.mindState?.duty?.def?.alwaysShowWeapon ?? false)))
            {
                if (!faction.HostileTo(map.ParentFaction))
                {
                    reader.proximity = proximity[0].grid;
                }
                else
                {
                    reader.proximity = proximity[1].grid;
                }                
            }
            return true;
        }        

        public void Notify_PawnSuppressed(IntVec3 cell)
        {         
            if (cell.InBounds(map))
            {
                danger.Flood(cell, 5, 4);                
            }
        }

        public void Notify_Bullet(IntVec3 cell)
        {
            if (cell.InBounds(map))
            {
                danger.Set(cell, 4, 3);
            }            
        }

        public void Notify_Smoke(IntVec3 cell)
        {            
            if (cell.InBounds(map))
            {
                danger.Flood(cell, 1.5f, 3);
            }
        }

        public void Notify_PathFound(Pawn pawn, PawnPath path)
        {
            if (pawn.Faction == null || map.ParentFaction == null)
            {
                return;
            }
            IShortTermMemoryHandler manager = !pawn.Faction.HostileTo(map.ParentFaction) ? proximity[0] : proximity[1];
            for (int i = 3; i < path.nodes.Count; i += 3)
            {
                manager.Set(path.nodes[i], 3, 4);
            }           
        }

        public void Notify_CoverPositionSelected(Pawn pawn, IntVec3 cell)
        {
            if (pawn.Faction == null || map.ParentFaction == null)
            {
                return;
            }
            if (cell.InBounds(map))
            {
                IShortTermMemoryHandler manager = !pawn.Faction.HostileTo(map.ParentFaction) ? proximity[0] : proximity[1];
                manager.Set(cell, 16f, 10f);                
            }
        }

        public void Notify_Injury(Pawn pawn, IntVec3 cell)
        {
            if (map.ParentFaction == null)
            {
                return;
            }
            if (cell.InBounds(map))
            {
                danger.Set(cell, 5, 7);              
            }
        }

        public void Notify_Death(Pawn pawn, IntVec3 cell)
        {
            if (map.ParentFaction == null)
            {
                return;
            }
            if (cell.InBounds(map))
            {
                danger.Set(cell, 8, 10);                
            }
        }
    }
}

