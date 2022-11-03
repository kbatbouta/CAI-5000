using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatAI
{
    public class SightTracker : MapComponent
    {
        private HashSet<IntVec3> _drawnCells = new HashSet<IntVec3>();

        public class SightReader
        {            
            public ISignalGrid friendly;
            public ISignalGrid hostile;
            public ISignalGrid universal;
            public ISignalGrid turrets;

            private readonly Map map;
            private readonly CellIndices indices;
            private readonly SightTracker tacker;

            public SightTracker Tacker
            {
                get => tacker;
            }
            public Map Map
            {
                get => map;
            }

            public SightReader(SightTracker tracker)
            {
                this.tacker = tracker;
                this.map = tracker.map;
                this.indices = tracker.map.cellIndices;
            }

            public float GetEnemies(IntVec3 cell) => GetEnemies(indices.CellToIndex(cell));
            public float GetEnemies(int index)
            {                
                float value = 0f;
                if (hostile != null)
                    value += hostile.GetSignalNum(index);
                if (turrets != null)
                    value += turrets.GetSignalNum(index);
                if (universal != null)
                    value += universal.GetSignalNum(index);
                return value;
            }

            public float GetFriendlies(IntVec3 cell) => GetFriendlies(indices.CellToIndex(cell));
            public float GetFriendlies(int index) => friendly != null ? friendly.GetSignalNum(index) : 0f;            

            public float GetVisibility(IntVec3 cell) => GetVisibility(indices.CellToIndex(cell));
            public float GetVisibility(int index)
            {
                float value = 0f;
                if (hostile != null)
                    value += hostile.GetSignalStrengthAt(index);
                if (turrets != null)
                    value += turrets.GetSignalStrengthAt(index);
                if (universal != null)
                    value += universal.GetSignalStrengthAt(index);
                return value;
            }

            public bool CheckFlags(IntVec3 cell, UInt64 flags) => CheckFlags(indices.CellToIndex(cell), flags);
            public bool CheckFlags(int index, UInt64 flags) => (flags & GetFlags(index)) == flags;

            public UInt64 GetFlags(IntVec3 cell) => GetFlags(indices.CellToIndex(cell));
            public UInt64 GetFlags(int index)
            {
                UInt64 value = 0;
                if (hostile != null)
                    value |= hostile.GetFlagsAt(index);
                if (turrets != null)
                    value |= turrets.GetFlagsAt(index);
                if (universal != null)
                    value |= universal.GetFlagsAt(index);
                return value;
            }

            public UInt64 GetFriendlyFlags(IntVec3 cell) => GetFriendlyFlags(indices.CellToIndex(cell));
            public UInt64 GetFriendlyFlags(int index)
            {
                UInt64 value = 0;
                if (friendly != null)
                    value |= friendly.GetFlagsAt(index);                
                return value;
            }

            public Vector2 GetDirection(IntVec3 cell) => GetDirection(indices.CellToIndex(cell));
            public Vector2 GetDirection(int index)
            {
                Vector2 value = Vector2.zero;
                if (hostile != null)
                    value += hostile.GetSignalDirectionAt(index);
                if (turrets != null)
                    value += turrets.GetSignalDirectionAt(index);
                if (universal != null)
                    value += universal.GetSignalDirectionAt(index);                
                return value;
            }

            public Vector2 GetFriendlyDirection(IntVec3 cell) => GetFriendlyDirection(indices.CellToIndex(cell));
            public Vector2 GetFriendlyDirection(int index) => friendly != null ? friendly.GetSignalDirectionAt(index) : Vector2.zero;          
        }

        public readonly SightHandler_Pawns friendly;
        public readonly SightHandler_Pawns hostile;
        public readonly SightHandler_Pawns universal;
        public readonly SightHandler_Turrets turrets;       
        
        public SightTracker(Map map) : base(map)
        {
            friendly =
                new SightHandler_Pawns(map, 20, 4);
            hostile =
                new SightHandler_Pawns(map, 20, 4);
            universal =
                new SightHandler_Pawns(map, 20, 10);
            turrets =
                new SightHandler_Turrets(map, 20, 100);            
        }        

        public override void MapComponentTick()
        {            
            base.MapComponentTick();
            // --------------
            friendly.Tick();
            // --------------
            hostile.Tick();
            // --------------
            universal.Tick();
            // --------------
            turrets.Tick();
            //
            // debugging stuff.
            if (GenTicks.TicksGame % 15 == 0 && Finder.Settings.Debug_DrawShadowCasts)
            {
                _drawnCells.Clear();
                if (!Find.Selector.SelectedPawns.NullOrEmpty())
                {
                    foreach (Pawn pawn in Find.Selector.SelectedPawns)
                    {
                        pawn.GetSightReader(out SightReader reader);
                        if (reader != null)
                        {
                            IntVec3 center = pawn.Position;
                            if (center.InBounds(map))
                            {
                                for (int i = center.x - 64; i < center.x + 64; i++)
                                {
                                    for (int j = center.z - 64; j < center.z + 64; j++)
                                    {
                                        IntVec3 cell = new IntVec3(i, 0, j);
                                        if (cell.InBounds(map) && !_drawnCells.Contains(cell))
                                        {
                                            _drawnCells.Add(cell);
                                            var value = reader.GetEnemies(cell);
                                            if (value > 0)
                                                map.debugDrawer.FlashCell(cell, Mathf.Clamp((float)reader.GetVisibility(cell) / 10f, 0f, 0.99f), $"{Math.Round(value, 3)} {value}", 15);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                    if (center.InBounds(map))
                    {
                        for (int i = center.x - 64; i < center.x + 64; i++)
                        {
                            for (int j = center.z - 64; j < center.z + 64; j++)
                            {
                                IntVec3 cell = new IntVec3(i, 0, j);
                                if (cell.InBounds(map) && !_drawnCells.Contains(cell))
                                {
                                    _drawnCells.Add(cell);
                                    var value = hostile.grid.GetSignalStrengthAt(cell, out int enemies1) + friendly.grid.GetSignalStrengthAt(cell, out int enemies2);
                                    if (value > 0)
                                        map.debugDrawer.FlashCell(cell, Mathf.Clamp(value / 10f, 0f, 0.99f), $"{Math.Round(value, 3)} {enemies1 + enemies2}", 15);
                                }
                            }
                        }
                    }
                }
                if (_drawnCells.Count > 0)
                    _drawnCells.Clear();
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (Finder.Settings.Debug_DrawShadowCastsVectors)
            {
                TurretTracker turretTracker = map.GetComponent<TurretTracker>();
                IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 24, true))
                    {
                        float enemies = hostile.grid.GetSignalNum(cell) + friendly.grid.GetSignalNum(cell);
                        Vector3 dir = hostile.grid.GetSignalDirectionAt(cell) + friendly.grid.GetSignalDirectionAt(cell);
                        if (cell.InBounds(map) && enemies > 0)
                        {
                            Vector2 direction = dir.normalized * 0.5f;
                            Vector2 start = UI.MapToUIPosition(cell.ToVector3Shifted());
                            Vector2 end = UI.MapToUIPosition(cell.ToVector3Shifted() + new Vector3(direction.x, 0, direction.y));
                            if (Vector2.Distance(start, end) > 1f
                                && start.x > 0
                                && start.y > 0
                                && end.x > 0
                                && end.y > 0
                                && start.x < UI.screenWidth
                                && start.y < UI.screenHeight
                                && end.x < UI.screenWidth
                                && end.y < UI.screenHeight)
                                Widgets.DrawLine(start, end, Color.white, 1);
                        }
                    }
                }
            }
        }

        public bool TryGetReader(Pawn pawn, out SightReader reader)
        {
            if (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Insect)
            {
                reader = new SightReader(this);
                reader.hostile = friendly.grid;
                reader.friendly = universal.grid;
                reader.turrets = turrets.grid;
                reader.universal = hostile.grid;
                return true;
            }
            return TryGetReader(pawn.Faction, out reader);
        }

        public bool TryGetReader(Faction faction, out SightReader reader)
        {
            if (faction == null)
            {
                reader = null;
                return false;
            }
            if (faction.def == FactionDefOf.Mechanoid || faction.def == FactionDefOf.Insect)
            {
                reader = new SightReader(this);
                reader.hostile = friendly.grid;
                reader.friendly = universal.grid;
                reader.turrets = turrets.grid;
                reader.universal = hostile.grid;
                return true;
            }
            reader = new SightReader(this);
            if (faction.HostileTo(map.ParentFaction))
            {                
                reader.hostile = friendly.grid;
                reader.friendly = hostile.grid;
                reader.turrets = turrets.grid;                
            }
            else
            {                
                reader.hostile = hostile.grid;
                reader.friendly = friendly.grid;
                reader.turrets = null;
            }
            reader.universal = universal.grid;
            return true;
        }

        public void Register(Building_TurretGun turret)
        {
            if(turret.Faction == map.ParentFaction)
            {
                turrets.DeRegister(turret);
                turrets.Register(turret);
            }
        }

        public void Register(Pawn pawn)
        {
            if (pawn.Faction == null)
                return;
            // make sure it's not already in.
            friendly.DeRegister(pawn);
            // make sure it's not already in.
            hostile.DeRegister(pawn);
            // make sure it's not already in.
            universal.DeRegister(pawn);

            if ((pawn.Faction.def == FactionDefOf.Insect || pawn.RaceProps.Insect) || (pawn.Faction.def == FactionDefOf.Mechanoid || pawn.RaceProps.IsMechanoid))
            {
                universal.Register(pawn);
                return;
            }
            // now register the new pawn.
            if (pawn.Faction?.HostileTo(map.ParentFaction) ?? true)
                hostile.Register(pawn);
            else
                friendly.Register(pawn);            
        }

        public void DeRegister(Building_TurretGun turret) => turrets.DeRegister(turret);
        public void DeRegister(Pawn pawn)
        {
            // cleanup hostiltes incase pawn switched factions.
            hostile.DeRegister(pawn);
            // cleanup friendlies incase pawn switched factions.
            friendly.DeRegister(pawn);
            // cleanup universals incase everything else fails.
            universal.DeRegister(pawn);           
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            // TODO redo this
            hostile.Notify_MapRemoved();
            // TODO redo this
            friendly.Notify_MapRemoved();
            // TODO redo this
            universal.Notify_MapRemoved();
        }
    }
}

