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
            public ISignalGrid[] friendlies;
            public ISignalGrid[] hostiles;                                  

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

            public SightReader(SightTracker tracker, ISignalGrid[] friendlies, ISignalGrid[] hostiles)
            {
                this.tacker = tracker;
                this.map = tracker.map;
                this.indices = tracker.map.cellIndices;
                this.friendlies = friendlies.ToArray();
                this.hostiles = hostiles.ToArray();
            }

            public float GetAbsVisibilityToEnemies(IntVec3 cell) => GetAbsVisibilityToEnemies(indices.CellToIndex(cell));
            public float GetAbsVisibilityToEnemies(int index)
            {                
                float value = 0f;
                for(int i = 0;i < hostiles.Length; i++)
                {
                    value += hostiles[i].GetSignalNum(index);
                }               
                return value;
            }

            public float GetAbsVisibilityToFriendlies(IntVec3 cell) => GetAbsVisibilityToFriendlies(indices.CellToIndex(cell));
            public float GetAbsVisibilityToFriendlies(int index)
            {
                float value = 0f;
                for (int i = 0; i < friendlies.Length; i++)
                {
                    value += friendlies[i].GetSignalNum(index);
                }
                return value;
            }            

            public float GetVisibilityToEnemies(IntVec3 cell) => GetVisibilityToEnemies(indices.CellToIndex(cell));
            public float GetVisibilityToEnemies(int index)
            {
                float value = 0f;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value += hostiles[i].GetSignalStrengthAt(index);
                }                
                return value;
            }

            public bool CheckFlags(IntVec3 cell, UInt64 flags) => CheckFlags(indices.CellToIndex(cell), flags);
            public bool CheckFlags(int index, UInt64 flags) => (flags & GetEnemyFlags(index)) == flags;

            public UInt64 GetEnemyFlags(IntVec3 cell) => GetEnemyFlags(indices.CellToIndex(cell));
            public UInt64 GetEnemyFlags(int index)
            {
                UInt64 value = 0;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value |= hostiles[i].GetFlagsAt(index);
                }
                return value;
            }

            public UInt64 GetFriendlyFlags(IntVec3 cell) => GetFriendlyFlags(indices.CellToIndex(cell));
            public UInt64 GetFriendlyFlags(int index)
            {
                UInt64 value = 0;
                for (int i = 0; i < friendlies.Length; i++)
                {
                    value |= friendlies[i].GetFlagsAt(index);
                }                
                return value;
            }

            public Vector2 GetEnemyDirection(IntVec3 cell) => GetEnemyDirection(indices.CellToIndex(cell));
            public Vector2 GetEnemyDirection(int index)
            {
                Vector2 value = Vector2.zero;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value += hostiles[i].GetSignalDirectionAt(index);
                }                
                return value;
            }

            public Vector2 GetFriendlyDirection(IntVec3 cell) => GetFriendlyDirection(indices.CellToIndex(cell));
            public Vector2 GetFriendlyDirection(int index)
            {
                Vector2 value = Vector2.zero;
                for (int i = 0; i < friendlies.Length; i++)
                {
                    value += friendlies[i].GetSignalDirectionAt(index);
                }
                return value;                
            }
        }

        public readonly SightHandler colonistsAndFriendlies;
        public readonly SightHandler raidersAndHostiles;
        public readonly SightHandler wildlifeAndMechs;
        public readonly SightHandler settlementTurrets;       
        
        public SightTracker(Map map) : base(map)
        {
            colonistsAndFriendlies =
                new SightHandler(this, 20, 4);
            raidersAndHostiles =
                new SightHandler(this, 20, 4);
            wildlifeAndMechs =
                new SightHandler(this, 20, 10);
            settlementTurrets =
                new SightHandler(this, 20, GenTicks.TickRareInterval);            
        }        

        public override void MapComponentTick()
        {            
            base.MapComponentTick();
            // --------------
            colonistsAndFriendlies.Tick();
            // --------------
            raidersAndHostiles.Tick();
            // --------------
            wildlifeAndMechs.Tick();
            // --------------
            settlementTurrets.Tick();
            //
            // debugging stuff.
            if (GenTicks.TicksGame % 15 == 0 && Finder.Settings.Debug_DrawShadowCasts)
            {
                _drawnCells.Clear();
                if (!Find.Selector.SelectedPawns.NullOrEmpty())
                {
                    foreach (Pawn pawn in Find.Selector.SelectedPawns)
                    {
                        TryGetReader(pawn, out SightReader reader);
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
                                            var value = reader.GetAbsVisibilityToEnemies(cell);
                                            if (value > 0)
                                                map.debugDrawer.FlashCell(cell, Mathf.Clamp((float)reader.GetVisibilityToEnemies(cell) / 10f, 0f, 0.99f), $"{Math.Round(value, 3)} {value}", 15);
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
                                    var value = raidersAndHostiles.grid.GetSignalStrengthAt(cell, out int enemies1) + colonistsAndFriendlies.grid.GetSignalStrengthAt(cell, out int enemies2) + settlementTurrets.grid.GetSignalStrengthAt(cell, out int enemies3) + wildlifeAndMechs.grid.GetSignalStrengthAt(cell, out int enemies4);
                                    if (value > 0)
                                    {
                                        map.debugDrawer.FlashCell(cell, Mathf.Clamp(value / 7f, 0.1f, 0.99f), $"{Math.Round(value, 3)} {enemies1 + enemies2}", 15);
                                    }
                                }
                            }
                        }
                    }
                }
                if (_drawnCells.Count > 0)
                {
                    _drawnCells.Clear();
                }
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
                        float enemies = settlementTurrets.grid.GetSignalNum(cell) + colonistsAndFriendlies.grid.GetSignalNum(cell);
                        Vector3 dir = colonistsAndFriendlies.grid.GetSignalDirectionAt(cell);
                        if (cell.InBounds(map) && enemies > 0)
                        {
                            Vector2 direction = dir * 0.25f;
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

        public bool TryGetReader(Pawn pawn, out SightReader reader) => TryGetReader(pawn.Faction, out reader);
        public bool TryGetReader(Faction faction, out SightReader reader)
        {           
            if (faction == null)
            {
                reader = new SightReader(this,
                    friendlies: new ISignalGrid[]
                    {
                    },
                    hostiles: new ISignalGrid[]
                    {
                        colonistsAndFriendlies.grid, wildlifeAndMechs.grid, raidersAndHostiles.grid, settlementTurrets.grid
                    });
                return true;
            }
            if (faction.def == FactionDefOf.Mechanoid || faction.def == FactionDefOf.Insect)
            {
                reader = new SightReader(this,
                    friendlies: new ISignalGrid[]
                    {
                        wildlifeAndMechs.grid
                    },
                    hostiles: new ISignalGrid[]
                    {
                        colonistsAndFriendlies.grid, raidersAndHostiles.grid, settlementTurrets.grid
                    });
                return true;
            }
            Faction mapFaction = map.ParentFaction;
            if ((mapFaction != null && !faction.HostileTo(map.ParentFaction)) || (mapFaction == null && Faction.OfPlayerSilentFail != null && !faction.HostileTo(Faction.OfPlayerSilentFail)))
            {
                reader = new SightReader(this,
                    friendlies: new ISignalGrid[]
                    {
                        colonistsAndFriendlies.grid, settlementTurrets.grid
                    },
                    hostiles: new ISignalGrid[]
                    {
                        raidersAndHostiles.grid, wildlifeAndMechs.grid
                    });
            }
            else
            {
                reader = new SightReader(this,
                    friendlies: new ISignalGrid[]
                    {
                        raidersAndHostiles.grid,
                    },
                    hostiles: new ISignalGrid[]
                    {
                        colonistsAndFriendlies.grid, settlementTurrets.grid, wildlifeAndMechs.grid
                    });
            }              
            return true;
        }

        public void Register(Thing thing)
        {
            if (thing is Pawn pawn)
            {
                Register(pawn);
            }
            else if(thing is Building_TurretGun turret)
            {
                Register(turret);
            }
            throw new NotImplementedException();
        }

        public void Register(Building_TurretGun turret)
        {
            Faction mapFaction = map.ParentFaction;
            if (turret.Faction != null && mapFaction != null && !turret.Faction.HostileTo(mapFaction))
            {
                raidersAndHostiles.TryDeRegister(turret);                
                settlementTurrets.Register(turret);
            }
            else if(map.ParentFaction != null && (turret.Faction?.HostileTo(map.ParentFaction) ?? false))
            {
                raidersAndHostiles.TryDeRegister(turret);
            }
        }

        public void Register(Pawn pawn)
        {           
            // make sure it's not already in.
            colonistsAndFriendlies.TryDeRegister(pawn);
            // make sure it's not already in.
            raidersAndHostiles.TryDeRegister(pawn);
            // make sure it's not already in.
            wildlifeAndMechs.TryDeRegister(pawn);

            Faction faction = pawn.Faction;
            Faction mapFaction = map.ParentFaction;
            if (faction == null || faction.def == FactionDefOf.Insect || faction.def == FactionDefOf.Mechanoid)
            {
                wildlifeAndMechs.Register(pawn);
            }
            else if ((mapFaction != null && !faction.HostileTo(mapFaction)) || (mapFaction == null && Faction.OfPlayerSilentFail != null && !faction.HostileTo(Faction.OfPlayerSilentFail)))
            {
                colonistsAndFriendlies.Register(pawn);
            }
            else
            {
                raidersAndHostiles.Register(pawn);
            }
        }

        public void DeRegister(Building_TurretGun turret)
        {            
            settlementTurrets.TryDeRegister(turret);
            raidersAndHostiles.TryDeRegister(turret);
        }

        public void DeRegister(Pawn pawn)
        {
            // cleanup hostiltes incase pawn switched factions.
            raidersAndHostiles.TryDeRegister(pawn);
            // cleanup friendlies incase pawn switched factions.
            colonistsAndFriendlies.TryDeRegister(pawn);
            // cleanup universals incase everything else fails.
            wildlifeAndMechs.TryDeRegister(pawn);           
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            // TODO redo this
            raidersAndHostiles.Notify_MapRemoved();
            // TODO redo this
            colonistsAndFriendlies.Notify_MapRemoved();
            // TODO redo this
            wildlifeAndMechs.Notify_MapRemoved();
        }
    }
}

