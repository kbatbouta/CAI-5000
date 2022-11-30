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
            public ArmorReport armor;
            
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

			public float GetThreat(IntVec3 cell) => GetThreat(indices.CellToIndex(cell));
			public float GetThreat(int index)
			{
                if (armor.weaknessAttributes != MetaCombatAttribute.None)
                {
                    MetaCombatAttribute attributes = GetMetaAttributes(index);
                    if ((attributes & armor.weaknessAttributes) != MetaCombatAttribute.None)
                    {
                        return 2.0f;
                    }
                }
				if (!Mod_CE.active)
                {
					return armor.createdAt != 0 ? Mathf.Clamp01(Maths.Max(GetBlunt(index) / (armor.Blunt + 0.001f), GetSharp(index) / (armor.Sharp + 0.001f), 0f)) : 0f;
                }
                else
                {
					return armor.createdAt != 0 ? Mathf.Clamp01(Maths.Max(GetBlunt(index) / (armor.Blunt + 0.001f), GetSharp(index) / (armor.Sharp + 0.001f), 0f)) : 0f;
				}
			}

			public float GetBlunt(IntVec3 cell) => GetBlunt(indices.CellToIndex(cell));
			public float GetBlunt(int index)
			{
				float value = 0f;
				for (int i = 0; i < hostiles.Length; i++)
				{
					value += hostiles[i].GetBlunt(index);
				}
				return value;
			}

			public float GetSharp(IntVec3 cell) => GetSharp(indices.CellToIndex(cell));
			public float GetSharp(int index)
			{
				float value = 0f;
				for (int i = 0; i < hostiles.Length; i++)
				{
					value += hostiles[i].GetSharp(index);
				}
				return value;
			}

			public MetaCombatAttribute GetMetaAttributes(IntVec3 cell) => GetMetaAttributes(indices.CellToIndex(cell));
			public MetaCombatAttribute GetMetaAttributes(int index)
			{
				MetaCombatAttribute value = MetaCombatAttribute.None;
				for (int i = 0; i < hostiles.Length; i++)
				{
					value |= hostiles[i].GetCombatAttributesAt(index);
				}
				return value;
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
		public readonly SightGrid wildlife;
        
        public readonly IThingsUInt64Map factionedUInt64Map;
        public readonly IThingsUInt64Map wildUInt64Map;

        public SightTracker(Map map) : base(map)
        {            
            colonistsAndFriendlies =
                new SightGrid(this, Finder.Settings.SightSettings_FriendliesAndRaiders);
            colonistsAndFriendlies.playerAlliance = true;
            colonistsAndFriendlies.trackFactions = true;

			raidersAndHostiles =
                new SightGrid(this, Finder.Settings.SightSettings_FriendliesAndRaiders);
            raidersAndHostiles.trackFactions = true;

			insectsAndMechs =
                new SightGrid(this, Finder.Settings.SightSettings_MechsAndInsects);
			insectsAndMechs.trackFactions = false;

			wildlife =
                new SightGrid(this, Finder.Settings.SightSettings_Wildlife);
            wildlife.trackFactions = false;

			factionedUInt64Map = new IThingsUInt64Map();
            wildUInt64Map = new IThingsUInt64Map();
        }        

        public override void MapComponentTick()
        {            
            base.MapComponentTick();
            int ticks = GenTicks.TicksGame;
            // --------------            
            colonistsAndFriendlies.SightGridTick();            
            // --------------
            if ((colonistsAndFriendlies.FactionNum > 1 && !Finder.Performance.TpsCriticallyLow) || ticks % 2 == 0)
            {
                raidersAndHostiles.SightGridTick();
            }
            // --------------
            if (!Finder.Performance.TpsCriticallyLow || ticks % 2 == 1)
            {
                insectsAndMechs.SightGridTick();
            }
            // --------------
            if (!Finder.Performance.TpsCriticallyLow || ticks % 3 == 2)
            {
                wildlife.SightGridTick();
            }
            //
            // debugging stuff.
            if ((Finder.Settings.Debug_DrawShadowCasts || Finder.Settings.Debug_DrawThreatCasts) && GenTicks.TicksGame % 15 == 0)
            {
                _drawnCells.Clear();
                if (!Find.Selector.SelectedPawns.NullOrEmpty())
                {
                    foreach (Pawn pawn in Find.Selector.SelectedPawns)
                    {
						ArmorReport report = ArmorUtility.GetArmorReport(pawn);
                        Log.Message($"{pawn}, t:{Math.Round(report.TankInt, 3)}, s:{report.bodySize}, bB:{Math.Round(report.bodyBlunt, 3)}, bS:{Math.Round(report.bodySharp, 3)}, aB:{Math.Round(report.apparelBlunt, 3)}, aS:{Math.Round(report.apparelSharp, 3)}, hs:{report.hasShieldBelt}");
						TryGetReader(pawn, out SightReader reader);
                        reader.armor = ArmorUtility.GetArmorReport(pawn);
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
                                            if (Finder.Settings.Debug_DrawThreatCasts)
                                            {
                                                var value = reader.GetThreat(cell);
                                                if (value > 0)
                                                    map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 2f), $"{Math.Round(value, 2)}", 15);
                                            }
											else if (Finder.Settings.Debug_DrawShadowCasts)
											{
												var value = reader.GetVisibilityToEnemies(cell);
												if (value > 0)
													map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 20f), $"{Math.Round(value, 2)}", 15);
											}
										}
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Finder.Settings.Debug_DrawShadowCasts)
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
                                    var value = raidersAndHostiles.grid.GetSignalStrengthAt(cell, out int enemies1) + colonistsAndFriendlies.grid.GetSignalStrengthAt(cell, out int enemies2)  + insectsAndMechs.grid.GetSignalStrengthAt(cell, out int enemies4);
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
            if (Finder.Settings.Debug_DrawShadowCasts)
            {
				Vector3 mousePos = UI.MouseMapPosition();
                Widgets.Label(new Rect(0, 0, 25, 200), $"{mousePos}");
			}
            Widgets.DrawBoxSolid(new Rect(0, 0, 3, 3), colonistsAndFriendlies.FactionNum == 1 ? Color.blue : (colonistsAndFriendlies.FactionNum > 1 ? Color.green : Color.yellow));
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

        public bool TryGetReader(Thing thing, out SightReader reader)
        {
            Faction faction = thing.Faction;
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
						colonistsAndFriendlies.grid, raidersAndHostiles.grid
					},
					neutrals: new ITSignalGrid[]
					{
						wildlife.grid
					});
				return true;
			}
			Faction playerFaction = Faction.OfPlayerSilentFail;
			if (playerFaction != null && !thing.HostileTo(playerFaction))
			{
				reader = new SightReader(this,
					friendlies: new ITSignalGrid[]
					{
						colonistsAndFriendlies.grid,
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
						colonistsAndFriendlies.grid, insectsAndMechs.grid
					},
					neutrals: new ITSignalGrid[]
					{
						wildlife.grid
					});
			}
			return true;
		}

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
                        colonistsAndFriendlies.grid, raidersAndHostiles.grid
                    },
                    neutrals: new ITSignalGrid[]
                    {
                        wildlife.grid
                    });
                return true;
            }
            Faction playerFaction = Faction.OfPlayerSilentFail;
			if (playerFaction != null && !faction.HostileTo(playerFaction))
            {                
				reader = new SightReader(this,
                    friendlies: new ITSignalGrid[]
                    {
                        colonistsAndFriendlies.grid,
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
                        colonistsAndFriendlies.grid, insectsAndMechs.grid
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
            // make sure it's not already in.
            factionedUInt64Map.Remove(thing);
            // make sure it's not already in.
            wildUInt64Map.Remove(thing);
            // make sure it's not already in.
            colonistsAndFriendlies.TryDeRegister(thing);
            // make sure it's not already in.
            raidersAndHostiles.TryDeRegister(thing);
            // make sure it's not already in.
            insectsAndMechs.TryDeRegister(thing);
            // make sure it's not already in.
            wildlife.TryDeRegister(thing);

            Faction faction = thing.Faction;
            Faction playerFaction;
            if (faction == null)
            {
                wildlife.Register(thing);
                wildUInt64Map.Add(thing);
            }
            else if (faction.def == FactionDefOf.Insect || faction.def == FactionDefOf.Mechanoid)
            {
                insectsAndMechs.Register(thing);
                factionedUInt64Map.Add(thing);
            }
            else if ((playerFaction = Faction.OfPlayerSilentFail) != null && !thing.HostileTo(playerFaction))
            {
                colonistsAndFriendlies.Register(thing);
                factionedUInt64Map.Add(thing);
            }
            else
            {
                raidersAndHostiles.Register(thing);
                factionedUInt64Map.Add(thing);
            }
            ThingComp_CombatAI comp = thing.GetComp_Fast<ThingComp_CombatAI>();
            if (comp != null)
            {
                TryGetReader(thing, out SightReader reader);
                comp.Notify_SightReaderChanged(reader);
            }
        }        

        public void DeRegister(Thing thing)
        {
            // cleanup factioned.
            factionedUInt64Map.Remove(thing);
            // cleanup wildlife.
            wildUInt64Map.Remove(thing);
            // cleanup hostiltes incase pawn switched factions.
            raidersAndHostiles.TryDeRegister(thing);
            // cleanup friendlies incase pawn switched factions.
            colonistsAndFriendlies.TryDeRegister(thing);
            // cleanup universals incase everything else fails.
            insectsAndMechs.TryDeRegister(thing);
            // cleanup universals incase everything else fails.
            wildlife.TryDeRegister(thing);
            // notify pawn comps.
            ThingComp_CombatAI comp = thing.GetComp_Fast<ThingComp_CombatAI>();
            if (comp != null)
            {
                comp.Notify_SightReaderChanged(null);
            }
        }

        public void Notify_PlayerRelationChanged(Faction faction)
        {
            List<Thing> things = new List<Thing>();
            things.AddRange(factionedUInt64Map.GetAllThings());
			things.AddRange(wildUInt64Map.GetAllThings());
            Faction player = Faction.OfPlayerSilentFail;
            if(player != null)
            {
                for(int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing.Faction == faction)
                    {
						this.DeRegister(thing);
						this.Register(thing);
					}
                }
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

