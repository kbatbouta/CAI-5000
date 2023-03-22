using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CombatAI.Comps;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public class SightTracker : MapComponent
    {
	    private readonly        HashSet<IntVec3> _drawnCells  = new HashSet<IntVec3>();
        private                 int              updateCounter;

        public readonly SightGrid        colonistsAndFriendlies;
        public readonly IThingsUInt64Map factionedUInt64Map;

        public readonly ITFloatGrid      fogGrid;
        public readonly SightGrid        insectsAndMechs;
        public readonly SightGrid        raidersAndHostiles;
        public readonly SightGrid        wildlife;
        public readonly IThingsUInt64Map wildUInt64Map;

        public SightTracker(Map map) : base(map)
        {
            fogGrid = new ITFloatGrid(map);
            colonistsAndFriendlies =
                new SightGrid(this, Finder.Settings.SightSettings_FriendliesAndRaiders, 0);
            colonistsAndFriendlies.gridFog        = fogGrid;
            colonistsAndFriendlies.playerAlliance = true;
            colonistsAndFriendlies.trackFactions  = true;

            raidersAndHostiles =
                new SightGrid(this, Finder.Settings.SightSettings_FriendliesAndRaiders, 1);
            raidersAndHostiles.trackFactions = true;

            insectsAndMechs =
                new SightGrid(this, Finder.Settings.SightSettings_MechsAndInsects, 2);
            insectsAndMechs.trackFactions = false;

            wildlife =
                new SightGrid(this, Finder.Settings.SightSettings_Wildlife, 3);
            wildlife.trackFactions = false;

            factionedUInt64Map = new IThingsUInt64Map();
            wildUInt64Map      = new IThingsUInt64Map();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            colonistsAndFriendlies.FinalizeInit();
            raidersAndHostiles.FinalizeInit();
            insectsAndMechs.FinalizeInit();
            wildlife.FinalizeInit();
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            updateCounter++;
            bool gamePaused      = false;
            bool performanceOkay = false;
            if (Find.TickManager != null)
            {
                gamePaused      = Find.TickManager.Paused;
                performanceOkay = Finder.Performance.TpsDeficit <= 5 * Find.TickManager.TickRateMultiplier;
            }
            // --------------
            colonistsAndFriendlies.SightGridUpdate();
            colonistsAndFriendlies.SightGridOptionalUpdate(gamePaused, performanceOkay);
            // --------------
            if (colonistsAndFriendlies.FactionNum > 1 || updateCounter % 3 == 0)
            {
                raidersAndHostiles.SightGridUpdate();
            }
            raidersAndHostiles.SightGridOptionalUpdate(gamePaused, performanceOkay);
            // --------------
            if ((!Finder.Performance.TpsCriticallyLow && colonistsAndFriendlies.FactionNum > 1) || updateCounter % 3 == 1)
            {
	            insectsAndMechs.SightGridUpdate();
            }
            insectsAndMechs.SightGridOptionalUpdate(gamePaused, performanceOkay);
            // --------------
            wildlife.SightGridUpdate();
            if (!Finder.Performance.TpsCriticallyLow || updateCounter % 3 == 2)
            {
	            wildlife.SightGridOptionalUpdate(gamePaused, performanceOkay);
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            // debugging stuff.
            if ((Finder.Settings.Debug_DrawShadowCasts || Finder.Settings.Debug_DrawThreatCasts || Finder.Settings.Debug_DebugAvailability) && GenTicks.TicksGame % 15 == 0)
            {
                _drawnCells.Clear();
                if (!Find.Selector.SelectedPawns.NullOrEmpty())
                {
                    foreach (Pawn pawn in Find.Selector.SelectedPawns)
                    {
                        //ArmorReport report = ArmorUtility.GetArmorReport(pawn);
                        //Log.Message($"{pawn}, t:{Math.Round(report.TankInt, 3)}, s:{report.bodySize}, bB:{Math.Round(report.bodyBlunt, 3)}, bS:{Math.Round(report.bodySharp, 3)}, aB:{Math.Round(report.apparelBlunt, 3)}, aS:{Math.Round(report.apparelSharp, 3)}, hs:{report.hasShieldBelt}");
                        TryGetReader(pawn, out SightReader reader);
                        reader.armor = pawn.GetArmorReport();
                        if (reader != null)
                        {
	                        if (Input.GetKey(KeyCode.LeftShift))
	                        {
		                        IntVec3 cell = reader.GetNearestEnemy(pawn.Position);
		                        if (cell.IsValid && cell.InBounds(map))
		                        {
			                        map.debugDrawer.FlashCell(cell, 0.99f, $"XXXX", 15);
			                        map.debugDrawer.FlashCell(cell, 0.99f, $"XXXX", 15);
		                        }
	                        }
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
                                                float value = reader.GetThreat(cell);
                                                if (value > 0)
                                                {
                                                    map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 20f), $"{Math.Round(value, 2)}", 15);
                                                }
                                                //var value = reader.hostiles[0].GetSharp(cell) + reader.hostiles[0].GetBlunt(cell);
                                                //if (value > 0)
                                                //	map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 4f), $"{Math.Round(reader.hostiles[0].GetSharp(cell), 2)} {Math.Round(reader.hostiles[0].GetBlunt(cell), 2)}", 15);
                                            }
                                            else if (Finder.Settings.Debug_DrawShadowCasts)
                                            {
                                                float value = reader.GetVisibilityToEnemies(cell);
                                                if (value > 0)
                                                {
                                                    map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 20f), $"{Math.Round(value, 2)}", 15);
                                                }
                                            }
                                            else if (Finder.Settings.Debug_DebugAvailability)
                                            {
	                                            if (!Input.GetKey(KeyCode.LeftShift))
	                                            {
		                                            float value = reader.GetEnemyAvailability(cell);
		                                            if (value > 0)
		                                            {
			                                            map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 20f), $"{Math.Round(value, 2)}", 15);
		                                            }
	                                            }
	                                            else
	                                            {
//		                                            List<Thing> store = new List<Thing>();
		                                            IntVec3     loc   = reader.GetNearestEnemy(cell);
		                                            if (loc.IsValid)
		                                            {
			                                            map.debugDrawer.FlashCell(loc, 0.99f, $"{loc}", 15);
		                                            }
	                                            }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Finder.Settings.Debug_DrawShadowCasts || Finder.Settings.Debug_DebugAvailability)
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
                                    if (Finder.Settings.Debug_DrawShadowCasts)
                                    {
                                        if (!Input.GetKey(KeyCode.LeftShift))
                                        {
                                            float value = raidersAndHostiles.grid.GetSignalStrengthAt(cell, out int enemies1) + colonistsAndFriendlies.grid.GetSignalStrengthAt(cell, out int enemies2) + insectsAndMechs.grid.GetSignalStrengthAt(cell, out int enemies4) + wildlife.grid.GetSignalStrengthAt(cell, out int enemies5);
                                            if (value > 0)
                                            {
                                                map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 20f), $"{Math.Round(value, 2)}", 15);
                                            }
                                            else
                                            {
                                                MetaCombatAttribute attr = raidersAndHostiles.grid.GetCombatAttributesAt(cell) | colonistsAndFriendlies.grid.GetCombatAttributesAt(cell) | insectsAndMechs.grid.GetCombatAttributesAt(cell);
                                                if ((attr & MetaCombatAttribute.Free) == MetaCombatAttribute.Free)
                                                {
                                                    map.debugDrawer.FlashCell(cell, 0.001f, "F", 15);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Region region = cell.GetRegion(map);
                                            float  value  = raidersAndHostiles.grid_regions.GetSignalNumByRegion(region) + colonistsAndFriendlies.grid_regions.GetSignalNumByRegion(region) + insectsAndMechs.grid_regions.GetSignalNumByRegion(region) + wildlife.grid_regions.GetSignalNumByRegion(region);
                                            if (value > 0)
                                            {
                                                map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 20f), $"{Math.Round(value, 2)}", 15);
                                            }
                                        }
                                    }
                                    else if (Finder.Settings.Debug_DebugAvailability)
                                    {
	                                    if (!Input.GetKey(KeyCode.LeftShift))
	                                    {
		                                    float value = raidersAndHostiles.grid.GetAvailability(cell) + colonistsAndFriendlies.grid.GetAvailability(cell) + insectsAndMechs.grid.GetAvailability(cell);
		                                    if (value > 0)
		                                    {
			                                    map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 20f), $"{Math.Round(value, 2)}", 15);
		                                    }
	                                    }
	                                    else
	                                    {
		                                    IntVec3 loc  = raidersAndHostiles.grid.GetNearestSourceAt(cell);
		                                    if (loc.IsValid)
		                                    {
			                                    float value = loc.DistanceTo(cell);
			                                    if (value > 0)
			                                    {
				                                    map.debugDrawer.FlashCell(cell, Mathf.Clamp01(value / 80), $"{loc}", 15);
			                                    }
		                                    }
	                                    }
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
            Widgets.DrawBoxSolid(new Rect(0, 0, 3, 3), colonistsAndFriendlies.FactionNum == 1 ? Color.blue : colonistsAndFriendlies.FactionNum > 1 ? Color.green : Color.yellow);
            if (Finder.Settings.Debug_DrawShadowCastsVectors)
            {
                TurretTracker turretTracker = map.GetComponent<TurretTracker>();
                IntVec3       center        = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 24, true))
                    {
                        float   enemies = raidersAndHostiles.grid.GetSignalNum(cell) + colonistsAndFriendlies.grid.GetSignalNum(cell);
                        Vector3 dir     = (colonistsAndFriendlies.grid.GetSignalDirectionAt(cell) + raidersAndHostiles.grid.GetSignalDirectionAt(cell)).normalized * 3;
                        if (cell.InBounds(map) && enemies > 0)
                        {
                            Vector2 direction = dir * 0.25f;
                            Vector2 start     = cell.ToVector3Shifted().MapToUIPosition();
                            Vector2 end       = (cell.ToVector3Shifted() + new Vector3(direction.x, 0, direction.y)).MapToUIPosition();
                            if (Vector2.Distance(start, end) > 1f
                                && start.x > 0
                                && start.y > 0
                                && end.x > 0
                                && end.y > 0
                                && start.x < UI.screenWidth
                                && start.y < UI.screenHeight
                                && end.x < UI.screenWidth
                                && end.y < UI.screenHeight)
                            {
                                Widgets.DrawLine(start, end, Color.white, 1);
                            }
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
                                         new SightGrid[]
                                         {
                                         },
                                         new[]
                                         {
                                             insectsAndMechs
                                         },
                                         new[]
                                         {
                                             wildlife, colonistsAndFriendlies, raidersAndHostiles
                                         });
                return true;
            }
            if (faction.def == FactionDefOf.Mechanoid || faction.def == FactionDefOf.Insect)
            {
                reader = new SightReader(this,
                                         new[]
                                         {
                                             insectsAndMechs
                                         },
                                         new[]
                                         {
                                             colonistsAndFriendlies, raidersAndHostiles
                                         },
                                         new[]
                                         {
                                             wildlife
                                         });
                return true;
            }
            Faction playerFaction = Faction.OfPlayerSilentFail;
            if (playerFaction != null && !thing.HostileTo(playerFaction))
            {
                reader = new SightReader(this,
                                         new[]
                                         {
                                             colonistsAndFriendlies
                                         },
                                         new[]
                                         {
                                             raidersAndHostiles, insectsAndMechs
                                         },
                                         new[]
                                         {
                                             wildlife
                                         });
            }
            else
            {
                reader = new SightReader(this,
                                         new[]
                                         {
                                             raidersAndHostiles
                                         },
                                         new[]
                                         {
                                             colonistsAndFriendlies, insectsAndMechs
                                         },
                                         new[]
                                         {
                                             wildlife
                                         });
            }
            return true;
        }

        public bool TryGetReader(Faction faction, out SightReader reader)
        {
            if (faction == null)
            {
                reader = new SightReader(this,
                                         new SightGrid[]
                                         {
                                         },
                                         new[]
                                         {
                                             insectsAndMechs
                                         },
                                         new[]
                                         {
                                             wildlife, colonistsAndFriendlies, raidersAndHostiles
                                         });
                return true;
            }
            if (faction.def == FactionDefOf.Mechanoid || faction.def == FactionDefOf.Insect)
            {
                reader = new SightReader(this,
                                         new[]
                                         {
                                             insectsAndMechs
                                         },
                                         new[]
                                         {
                                             colonistsAndFriendlies, raidersAndHostiles
                                         },
                                         new[]
                                         {
                                             wildlife
                                         });
                return true;
            }
            Faction playerFaction = Faction.OfPlayerSilentFail;
            if (playerFaction != null && !faction.HostileTo(playerFaction))
            {
                reader = new SightReader(this,
                                         new[]
                                         {
                                             colonistsAndFriendlies
                                         },
                                         new[]
                                         {
                                             raidersAndHostiles, insectsAndMechs
                                         },
                                         new[]
                                         {
                                             wildlife
                                         });
            }
            else
            {
                reader = new SightReader(this,
                                         new[]
                                         {
                                             raidersAndHostiles
                                         },
                                         new[]
                                         {
                                             colonistsAndFriendlies, insectsAndMechs
                                         },
                                         new[]
                                         {
                                             wildlife
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
            if (player != null)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing.Faction == faction)
                    {
                        DeRegister(thing);
                        Register(thing);
                    }
                }
            }
        }

        public void Notify_RegionChanged(IntVec3 cell, Region region)
        {
            colonistsAndFriendlies.grid_regions.SetRegionAt(cell, region);
            raidersAndHostiles.grid_regions.SetRegionAt(cell, region);
            insectsAndMechs.grid_regions.SetRegionAt(cell, region);
            wildlife.grid_regions.SetRegionAt(cell, region);
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

        public class SightReader
        {
	        private readonly CellIndices indices;

            public          ArmorReport    armor;
            public readonly ITSignalGrid[] friendlies;
            public readonly ITRegionGrid[] friendlies_regions;
            public readonly ITSignalGrid[] hostiles;
            public readonly ITRegionGrid[] hostiles_regions;
            public readonly ITSignalGrid[] neutrals;

            private readonly SightGrid[] hSight;
            private readonly SightGrid[] fSight;
            private readonly SightGrid[] nSight;

            public SightReader(SightTracker tracker, SightGrid[] friendlies, SightGrid[] hostiles, SightGrid[] neutrals)
            {
                Tracker               = tracker;
                Map                   = tracker.map;
                indices               = tracker.map.cellIndices;
                this.hSight           = hostiles;
                this.hostiles         = new ITSignalGrid[hostiles.Length];
                this.hostiles_regions = new ITRegionGrid[hostiles.Length];
                for (int i = 0; i < hostiles.Length; i++)
                {
	                this.hostiles[i]         = hostiles[i].grid;
	                this.hostiles_regions[i] = hostiles[i].grid_regions;
                }
                this.fSight              = friendlies;
                this.friendlies         = new ITSignalGrid[friendlies.Length];
                this.friendlies_regions = new ITRegionGrid[friendlies.Length];
                for (int i = 0; i < friendlies.Length; i++)
                {
	                this.friendlies[i]         = friendlies[i].grid;
	                this.friendlies_regions[i] = friendlies[i].grid_regions;
                }
                this.nSight    = neutrals;
                this.neutrals = new ITSignalGrid[neutrals.Length];
                for (int i = 0; i < neutrals.Length; i++)
                {
	                this.neutrals[i] = neutrals[i].grid;
                }
            }
            
            public SightTracker Tracker
            {
                get;
            }
            public Map Map
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetThreat(IntVec3 cell)
            {
                return GetThreat(indices.CellToIndex(cell));
            }
            public float GetThreat(int index)
            {
                MetaCombatAttribute attributes = GetMetaAttributes(index);
                float               val;
                if ((attributes & armor.weaknessAttributes) != MetaCombatAttribute.None)
                {
                    val = 2.0f;
                }
                else
                {
                    if (!Mod_CE.active)
                    {
                        val = armor.createdAt != 0 ? Mathf.Clamp01(2f * Maths.Max(GetBlunt(index) / (armor.Blunt + 0.001f), GetSharp(index) / (armor.Sharp + 0.001f), 0f)) : 0f;
                    }
                    else
                    {
                        val = armor.createdAt != 0 ? Mathf.Clamp01(Maths.Max(GetBlunt(index) / (armor.Blunt + 0.001f), GetSharp(index) / (armor.Sharp + 0.001f), 0f)) : 0f;
                    }
                }
                return val;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetBlunt(IntVec3 cell)
            {
                return GetBlunt(indices.CellToIndex(cell));
            }
            public float GetBlunt(int index)
            {
                float value = 0f;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value += hostiles[i].GetBlunt(index);
                }
                return value;
            }

            public int GetEnemyAvailability(IntVec3 cell)
            {
                return GetEnemyAvailability(indices.CellToIndex(cell));
            }
            public int GetEnemyAvailability(int index)
            {
                int value = 0;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value += hostiles[i].GetAvailability(index);
                }
                return value;
            }
            public int GetFriendlyAvailability(IntVec3 cell)
            {
                return GetFriendlyAvailability(indices.CellToIndex(cell));
            }
            public int GetFriendlyAvailability(int index)
            {
                int value = 0;
                for (int i = 0; i < friendlies.Length; i++)
                {
                    value += friendlies[i].GetAvailability(index);
                }
                return value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetSharp(IntVec3 cell)
            {
                return GetSharp(indices.CellToIndex(cell));
            }
            public float GetSharp(int index)
            {
                float value = 0f;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value += hostiles[i].GetSharp(index);
                }
                return value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public MetaCombatAttribute GetMetaAttributes(IntVec3 cell)
            {
                return GetMetaAttributes(indices.CellToIndex(cell));
            }
            public MetaCombatAttribute GetMetaAttributes(int index)
            {
                MetaCombatAttribute value = MetaCombatAttribute.None;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value |= hostiles[i].GetCombatAttributesAt(index);
                }
                return value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetRegionAbsVisibilityToEnemies(Region region)
            {
                if (region != null)
                {
                    int value = 0;
                    for (int i = 0; i < hostiles_regions.Length; i++)
                    {
                        value += hostiles_regions[i].GetSignalNumByRegion(region);
                    }
                    return value;
                }
                return 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetRegionAbsVisibilityToFriendlies(Region region)
            {
                if (region != null)
                {
                    int value = 0;
                    for (int i = 0; i < friendlies_regions.Length; i++)
                    {
                        value += friendlies_regions[i].GetSignalNumByRegion(region);
                    }
                    return value;
                }
                return 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetAbsVisibilityToNeutrals(IntVec3 cell)
            {
                return GetAbsVisibilityToNeutrals(indices.CellToIndex(cell));
            }
            public float GetAbsVisibilityToNeutrals(int index)
            {
                float value = 0f;
                for (int i = 0; i < neutrals.Length; i++)
                {
                    value += neutrals[i].GetSignalNum(index);
                }
                return value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetAbsVisibilityToEnemies(IntVec3 cell)
            {
                return GetAbsVisibilityToEnemies(indices.CellToIndex(cell));
            }
            public float GetAbsVisibilityToEnemies(int index)
            {
                float value = 0f;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value += hostiles[i].GetSignalNum(index);
                }
                return value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IntVec3 GetNearestEnemy(int index)
            {
	            return GetNearestEnemy(indices.IndexToCell(index));
            }
            public IntVec3 GetNearestEnemy(IntVec3 pos)
            {
	            IntVec3 result = IntVec3.Invalid;
	            int     min    = 999 * 999;
	            for (int i = 0; i < hostiles.Length; i++)
	            {
		            IntVec3 cell = hostiles[i].GetNearestSourceAt(pos);
		            if (cell.IsValid)
		            {
			            int dist = cell.DistanceToSquared(pos);
			            if (dist < min)
			            {
				            min    = dist;
				            result = cell;
			            }
		            }
	            }
	            return result;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IntVec3 GetNearestEnemy(int index, List<Thing> store)
            {
	            return GetNearestEnemy(indices.IndexToCell(index), store);
            }
            public IntVec3 GetNearestEnemy(IntVec3 pos, List<Thing> store)
            {
	            IntVec3 result = GetNearestEnemy(pos);
	            if (result.IsValid)
	            {
		            ulong flags = GetStaticEnemyFlags(result);
		            if (flags != 0)
		            {
			            for (int i = 0; i < hSight.Length; i++)
			            {
				            hSight[i].GetThings(flags, result, store);
			            }
		            }
	            }
	            return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetAbsVisibilityToFriendlies(IntVec3 cell)
            {
                return GetAbsVisibilityToFriendlies(indices.CellToIndex(cell));
            }
            public float GetAbsVisibilityToFriendlies(int index)
            {
                float value = 0f;
                for (int i = 0; i < friendlies.Length; i++)
                {
                    value += friendlies[i].GetSignalNum(index);
                }
                return value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetVisibilityToNeutrals(IntVec3 cell)
            {
                return GetVisibilityToNeutrals(indices.CellToIndex(cell));
            }
            public float GetVisibilityToNeutrals(int index)
            {
                float value = 0f;
                for (int i = 0; i < neutrals.Length; i++)
                {
                    value += neutrals[i].GetSignalStrengthAt(index);
                }
                return value;
            }
            public void GetEnemies(IntVec3 cell, List<Thing> store)
            {
	            ulong flags = GetDynamicEnemyFlags(cell);
	            if (flags != 0)
	            {
		            for (int i = 0; i < hSight.Length; i++)
		            {
			            hSight[i].GetThings(flags, cell, store);
		            }
	            }
            }
            public void GetFriendlies(IntVec3 cell, List<Thing> store)
            {
	            ulong flags = GetDynamicFriendlyFlags(cell);
	            if (flags != 0)
	            {
		            for (int i = 0; i < fSight.Length; i++)
		            {
			            fSight[i].GetThings(flags, cell, store);
		            }
	            }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetVisibilityToEnemies(IntVec3 cell)
            {
                return GetVisibilityToEnemies(indices.CellToIndex(cell));
            }
            public float GetVisibilityToEnemies(int index)
            {
                float value = 0f;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value += hostiles[i].GetSignalStrengthAt(index);
                }
                return value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetVisibilityToFriendlies(IntVec3 cell)
            {
                return GetVisibilityToFriendlies(indices.CellToIndex(cell));
            }
            public float GetVisibilityToFriendlies(int index)
            {
                float value = 0f;
                for (int i = 0; i < friendlies.Length; i++)
                {
                    value += friendlies[i].GetSignalStrengthAt(index);
                }
                return value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong GetStaticEnemyFlags(IntVec3 cell)
            {
                return GetStaticEnemyFlags(indices.CellToIndex(cell));
            }
            public ulong GetStaticEnemyFlags(int index)
            {
                ulong value = 0;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value |= hostiles[i].GetStaticFlagsAt(index);
                }
                return value;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong GetDynamicEnemyFlags(IntVec3 cell)
            {
	            return GetDynamicEnemyFlags(indices.CellToIndex(cell));
            }
            public ulong GetDynamicEnemyFlags(int index)
            {
	            ulong value = 0;
	            for (int i = 0; i < hostiles.Length; i++)
	            {
		            value |= hostiles[i].GetDynamicFlagsAt(index);
	            }
	            return value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong GetStaticFriendlyFlags(IntVec3 cell)
            {
                return GetStaticFriendlyFlags(indices.CellToIndex(cell));
            }
            public ulong GetStaticFriendlyFlags(int index)
            {
                ulong value = 0;
                for (int i = 0; i < friendlies.Length; i++)
                {
                    value |= friendlies[i].GetStaticFlagsAt(index);
                }
                return value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong GetDynamicFriendlyFlags(IntVec3 cell)
            {
	            return GetDynamicFriendlyFlags(indices.CellToIndex(cell));
            }
            public ulong GetDynamicFriendlyFlags(int index)
            {
	            ulong value = 0;
	            for (int i = 0; i < friendlies.Length; i++)
	            {
		            value |= friendlies[i].GetDynamicFlagsAt(index);
	            }
	            return value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector2 GetEnemyDirection(IntVec3 cell)
            {
                return GetEnemyDirection(indices.CellToIndex(cell));
            }
            public Vector2 GetEnemyDirection(int index)
            {
                Vector2 value = Vector2.zero;
                for (int i = 0; i < hostiles.Length; i++)
                {
                    value -= hostiles[i].GetSignalDirectionAt(index);
                }
                return value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector2 GetFriendlyDirection(IntVec3 cell)
            {
                return GetFriendlyDirection(indices.CellToIndex(cell));
            }
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
    }
}
