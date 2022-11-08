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
            public ISignalGrid[] friendlies;
            public ISignalGrid[] hostiles;
            public ISignalGrid[] neutrals;

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

            public SightReader(SightTracker tracker, ISignalGrid[] friendlies, ISignalGrid[] hostiles, ISignalGrid[] neutrals)
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

        public readonly SightHandler colonistsAndFriendlies;
        public readonly SightHandler raidersAndHostiles;
        public readonly SightHandler insectsAndMechs;
        public readonly SightHandler settlementTurrets;
        public readonly SightHandler wildlife;

        public readonly FlagedBuckets factionedIndices;
        public readonly FlagedBuckets wildIndices;

        public SightTracker(Map map) : base(map)
        {
            colonistsAndFriendlies =
                new SightHandler(this, Finder.Settings.SightSettings_FriendliesAndRaiders);
            raidersAndHostiles =
                new SightHandler(this, Finder.Settings.SightSettings_FriendliesAndRaiders);
            insectsAndMechs =
                new SightHandler(this, Finder.Settings.SightSettings_MechsAndInsects);
            wildlife =
                new SightHandler(this, Finder.Settings.SightSettings_Wildlife);
            settlementTurrets =
                new SightHandler(this, Finder.Settings.SightSettings_SettlementTurrets);

            factionedIndices = new FlagedBuckets();
            wildIndices = new FlagedBuckets();
        }        

        public override void MapComponentTick()
        {            
            base.MapComponentTick();
            // --------------
            colonistsAndFriendlies.Tick();
            // --------------
            raidersAndHostiles.Tick();
            // --------------
            insectsAndMechs.Tick();
            // --------------
            settlementTurrets.Tick();
            // --------------
            wildlife.Tick();
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
                        insectsAndMechs.grid,
                    },
                    neutrals: new ISignalGrid[]
                    {
                        wildlife.grid, colonistsAndFriendlies.grid, raidersAndHostiles.grid
                    });
                return true;
            }
            if (faction.def == FactionDefOf.Mechanoid || faction.def == FactionDefOf.Insect)
            {
                reader = new SightReader(this,
                    friendlies: new ISignalGrid[]
                    {
                        insectsAndMechs.grid
                    },
                    hostiles: new ISignalGrid[]
                    {
                        colonistsAndFriendlies.grid, raidersAndHostiles.grid, settlementTurrets.grid
                    },
                    neutrals: new ISignalGrid[]
                    {
                        wildlife.grid
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
                        raidersAndHostiles.grid, insectsAndMechs.grid
                    },
                    neutrals: new ISignalGrid[]
                    {
                        wildlife.grid
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
                        colonistsAndFriendlies.grid, settlementTurrets.grid, insectsAndMechs.grid
                    },
                    neutrals: new ISignalGrid[]
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
            if (turret.Faction != null && mapFaction != null && !turret.Faction.HostileTo(mapFaction))
            {
                raidersAndHostiles.TryDeRegister(turret);                
                settlementTurrets.Register(turret);                                
            }
            else if(map.ParentFaction != null && (turret.Faction?.HostileTo(map.ParentFaction) ?? false))
            {
                raidersAndHostiles.TryDeRegister(turret);
            }
            factionedIndices.Add(turret);
        }

        public void Register(Pawn pawn)
        {
            // make sure it's not already in.
            factionedIndices.Remove(pawn);
            // make sure it's not already in.
            wildIndices.Remove(pawn);
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
                wildIndices.Add(pawn);
            }
            else if (faction.def == FactionDefOf.Insect || faction.def == FactionDefOf.Mechanoid)
            {
                insectsAndMechs.Register(pawn);
                factionedIndices.Add(pawn);
            }
            else if ((mapFaction != null && !faction.HostileTo(mapFaction)) || (mapFaction == null && Faction.OfPlayerSilentFail != null && !faction.HostileTo(Faction.OfPlayerSilentFail)))
            {
                colonistsAndFriendlies.Register(pawn);
                factionedIndices.Add(pawn);
            }
            else
            {
                raidersAndHostiles.Register(pawn);
                factionedIndices.Add(pawn);
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
            factionedIndices.Remove(turret);            
        }

        public void DeRegister(Pawn pawn)
        {
            // cleanup factioned.
            factionedIndices.Remove(pawn);
            // cleanup wildlife.
            wildIndices.Remove(pawn);
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
            wildIndices.Clear();
            // TODO redo this
            factionedIndices.Clear();

            base.MapRemoved();
            // TODO redo this
            raidersAndHostiles.Notify_MapRemoved();
            // TODO redo this
            colonistsAndFriendlies.Notify_MapRemoved();
            // TODO redo this
            insectsAndMechs.Notify_MapRemoved();
            // TODO redo this
            wildlife.Notify_MapRemoved();
        }
    }
}

