using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CombatAI.Comps;
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
            public ITSignalGrid[] friendlies;
            public ITSignalGrid[] hostiles;
            public ITSignalGrid[] neutrals;

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

            public SightReader(SightTracker tracker, ITSignalGrid[] friendlies, ITSignalGrid[] hostiles, ITSignalGrid[] neutrals)
            {
                this.tacker = tracker;
                this.map = tracker.map;
                this.indices = tracker.map.cellIndices;
                this.friendlies = friendlies.ToArray();
                this.hostiles = hostiles.ToArray();
                this.neutrals = neutrals.ToArray();
            }

            public float GetAbsVisibilityToNeutrals(IntVec3 cell) => GetAbsVisibilityToNeutrals(indices.CellToIndex(cell));
            public float GetAbsVisibilityToNeutrals(int index)
            {
                float value = 0f;
                for (int i = 0; i < neutrals.Length; i++)
                {
                    value += neutrals[i].GetSignalNum(index);
                }
                return value;
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

            public float GetVisibilityToNeutrals(IntVec3 cell) => GetVisibilityToNeutrals(indices.CellToIndex(cell));
            public float GetVisibilityToNeutrals(int index)
            {
                float value = 0f;
                for (int i = 0; i < neutrals.Length; i++)
                {
                    value += neutrals[i].GetSignalStrengthAt(index);
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

            public float GetVisibilityToFriendlies(IntVec3 cell) => GetVisibilityToFriendlies(indices.CellToIndex(cell));
            public float GetVisibilityToFriendlies(int index)
            {
                float value = 0f;
                for (int i = 0; i < friendlies.Length; i++)
                {
                    value += friendlies[i].GetSignalStrengthAt(index);
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
                    value -= hostiles[i].GetSignalDirectionAt(index);
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

        public readonly SightGrid colonistsAndFriendlies;
        public readonly SightGrid raidersAndHostiles;
        public readonly SightGrid insectsAndMechs;
        public readonly SightGrid settlementTurrets;
        public readonly SightGrid wildlife;
        
        public readonly IThingsUInt64Map factionedUInt64Map;
        public readonly IThingsUInt64Map wildUInt64Map;

        public SightTracker(Map map) : base(map)
        {
            colonistsAndFriendlies =
                new SightGrid(this, Finder.Settings.SightSettings_FriendliesAndRaiders);
            raidersAndHostiles =
                new SightGrid(this, Finder.Settings.SightSettings_FriendliesAndRaiders);
            insectsAndMechs =
                new SightGrid(this, Finder.Settings.SightSettings_MechsAndInsects);
            wildlife =
                new SightGrid(this, Finder.Settings.SightSettings_Wildlife);
            settlementTurrets =
                new SightGrid(this, Finder.Settings.SightSettings_SettlementTurrets);
            
            factionedUInt64Map = new IThingsUInt64Map();
            wildUInt64Map = new IThingsUInt64Map();
        }        

        public override void MapComponentTick()
        {            
            base.MapComponentTick();
            // --------------
            colonistsAndFriendlies.SightGridTick();
            // --------------
            raidersAndHostiles.SightGridTick();
            // --------------
            insectsAndMechs.SightGridTick();
            // --------------
            settlementTurrets.SightGridTick();
            // --------------
            wildlife.SightGridTick();
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
                                    var value = raidersAndHostiles.grid.GetSignalStrengthAt(cell, out int enemies1) + colonistsAndFriendlies.grid.GetSignalStrengthAt(cell, out int enemies2) + settlementTurrets.grid.GetSignalStrengthAt(cell, out int enemies3) + insectsAndMechs.grid.GetSignalStrengthAt(cell, out int enemies4);
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
                        float enemies = raidersAndHostiles.grid.GetSignalNum(cell) + colonistsAndFriendlies.grid.GetSignalNum(cell);
                        Vector3 dir = (colonistsAndFriendlies.grid.GetSignalDirectionAt(cell) + raidersAndHostiles.grid.GetSignalDirectionAt(cell)).normalized * 3;
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
                    friendlies: new ITSignalGrid[]
                    {
                    },
                    hostiles: new ITSignalGrid[]
                    {
                        insectsAndMechs.grid,
                    },
                    neutrals: new ITSignalGrid[]
                    {
                        wildlife.grid, colonistsAndFriendlies.grid, raidersAndHostiles.grid
                    });
                return true;
            }
            if (faction.def == FactionDefOf.Mechanoid || faction.def == FactionDefOf.Insect)
            {
                reader = new SightReader(this,
                    friendlies: new ITSignalGrid[]
                    {
                        insectsAndMechs.grid
                    },
                    hostiles: new ITSignalGrid[]
                    {
                        colonistsAndFriendlies.grid, raidersAndHostiles.grid, settlementTurrets.grid
                    },
                    neutrals: new ITSignalGrid[]
                    {
                        wildlife.grid
                    });
                return true;
            }
            Faction mapFaction = map.ParentFaction;            
            if ((mapFaction != null && !faction.HostileTo(map.ParentFaction)) || (mapFaction == null && Faction.OfPlayerSilentFail != null && !faction.HostileTo(Faction.OfPlayerSilentFail)))
            {
                reader = new SightReader(this,
                    friendlies: new ITSignalGrid[]
                    {
                        colonistsAndFriendlies.grid, settlementTurrets.grid
                    },
                    hostiles: new ITSignalGrid[]
                    {
                        raidersAndHostiles.grid, insectsAndMechs.grid
                    },
                    neutrals: new ITSignalGrid[]
                    {
                        wildlife.grid
                    });
            }
            else
            {
                reader = new SightReader(this,
                    friendlies: new ITSignalGrid[]
                    {
                        raidersAndHostiles.grid,
                    },
                    hostiles: new ITSignalGrid[]
                    {
                        colonistsAndFriendlies.grid, settlementTurrets.grid, insectsAndMechs.grid
                    },
                    neutrals: new ITSignalGrid[]
                    {
                        wildlife.grid
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
            if (turret.Faction != null && mapFaction != null && !turret.HostileTo(mapFaction))
            {
                raidersAndHostiles.TryDeRegister(turret);                
                settlementTurrets.Register(turret);                                
            }
            else if(map.ParentFaction != null && turret.HostileTo(map.ParentFaction))
            {
                raidersAndHostiles.TryDeRegister(turret);
            }
            factionedUInt64Map.Add(turret);
        }

        public void Register(Pawn pawn)
        {
            // make sure it's not already in.
            factionedUInt64Map.Remove(pawn);
            // make sure it's not already in.
            wildUInt64Map.Remove(pawn);
            // make sure it's not already in.
            colonistsAndFriendlies.TryDeRegister(pawn);
            // make sure it's not already in.
            raidersAndHostiles.TryDeRegister(pawn);
            // make sure it's not already in.
            insectsAndMechs.TryDeRegister(pawn);
            // make sure it's not already in.
            wildlife.TryDeRegister(pawn);

            Faction faction = pawn.Faction;
            Faction mapFaction = map.ParentFaction;
            if (faction == null)
            {
                wildlife.Register(pawn);
                wildUInt64Map.Add(pawn);
            }
            else if (faction.def == FactionDefOf.Insect || faction.def == FactionDefOf.Mechanoid)
            {
                insectsAndMechs.Register(pawn);
                factionedUInt64Map.Add(pawn);
            }
            else if ((mapFaction != null && !pawn.HostileTo(mapFaction)) || (mapFaction == null && Faction.OfPlayerSilentFail != null && !pawn.HostileTo(Faction.OfPlayerSilentFail)))
            {
                colonistsAndFriendlies.Register(pawn);
                factionedUInt64Map.Add(pawn);
            }
            else
            {
                raidersAndHostiles.Register(pawn);
                factionedUInt64Map.Add(pawn);
            }
            ThingComp_CombatAI comp = pawn.GetComp_Fast<ThingComp_CombatAI>();
            if (comp != null)
            {
                TryGetReader(pawn, out SightReader reader);
                comp.Notify_SightReaderChanged(reader);
            }
        }

        public void DeRegister(Building_TurretGun turret)
        {            
            settlementTurrets.TryDeRegister(turret);
            raidersAndHostiles.TryDeRegister(turret);
            factionedUInt64Map.Remove(turret);            
        }

        public void DeRegister(Pawn pawn)
        {
            // cleanup factioned.
            factionedUInt64Map.Remove(pawn);
            // cleanup wildlife.
            wildUInt64Map.Remove(pawn);
            // cleanup hostiltes incase pawn switched factions.
            raidersAndHostiles.TryDeRegister(pawn);
            // cleanup friendlies incase pawn switched factions.
            colonistsAndFriendlies.TryDeRegister(pawn);
            // cleanup universals incase everything else fails.
            insectsAndMechs.TryDeRegister(pawn);
            // cleanup universals incase everything else fails.
            wildlife.TryDeRegister(pawn);
            // notify pawn comps.
            ThingComp_CombatAI comp = pawn.GetComp_Fast<ThingComp_CombatAI>();
            if (comp != null)
            {
                comp.Notify_SightReaderChanged(null);
            }
        }

        public override void MapRemoved()
        {
            // TODO redo this
            wildUInt64Map.Clear();
            // TODO redo this
            factionedUInt64Map.Clear();

            base.MapRemoved();
            // TODO redo this
            raidersAndHostiles.Destroy();
            // TODO redo this
            colonistsAndFriendlies.Destroy();
            // TODO redo this
            insectsAndMechs.Destroy();
            // TODO redo this
            wildlife.Destroy();
        }
    }
}

