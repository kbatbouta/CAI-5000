using System;
using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine.SocialPlatforms;
using RimWorld.Planet;
using System.Linq;

namespace CombatAI
{
	public static class DebugActions_BattleRoyale
	{
		static List<Pair<BattleSpawn, string>> spawns = new List<Pair<BattleSpawn, string>>();

		static DebugActions_BattleRoyale()
		{
			spawns.Add(new Pair<BattleSpawn, string>(BattleSpawn.random, "random edge spawn"));
			spawns.Add(new Pair<BattleSpawn, string>(BattleSpawn.randomMapCenterAllowed, "random spawn - center allowed"));
			spawns.Add(new Pair<BattleSpawn, string>(BattleSpawn.rhsMapCenterAllowed, "rhs map center allowed"));
			spawns.Add(new Pair<BattleSpawn, string>(BattleSpawn.lhsMapCenterAllowed, "lhs map center allowed"));
			spawns.Add(new Pair<BattleSpawn, string>(BattleSpawn.rhsMapCenterAlways, "rhs map center always"));
			spawns.Add(new Pair<BattleSpawn, string>(BattleSpawn.lhsMapCenterAlways, "lhs map center always"));
		}

		[DebugAction("Cai-5000", "Start battleroyale", false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
		private static void StartBattle()
		{
			List<DebugMenuOption> items = new List<DebugMenuOption>();
			foreach (var item in spawns)
			{
				items.Add(new DebugMenuOption(item.second, DebugMenuOptionMode.Action, delegate
				{
					Log.Message($"CAI battleroyale config: spawn_type: {item.first}");
					StartBattleInternel(item.first);					
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(items));
		}
		
		private static void StartBattleInternel(BattleSpawn spawn)
		{
			List<DebugMenuOption> items = new List<DebugMenuOption>();
			for (int i = 1; i < 32; i += 2)
			{ 
				items.Add(new DebugMenuOption($"rounds num per pair: {i}", DebugMenuOptionMode.Action, delegate
				{
					Log.Message($"CAI battleroyale config: spawn_type: {spawn}, rounds_per_pair:{i}");
					GenerateNewMaps();
					BattleRoyale.roundsPerPair = i;
					BattleRoyale.spawn = spawn;
					BattleRoyale.manager.Start();
					DebugViewSettings.neverForceNormalSpeed = true;
					if (Find.TickManager.Paused)
					{												
						Find.TickManager.TogglePaused();												
					}
					TickManager.UltraSpeedBoost = true;
					Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(items));
		}

		private static void GenerateNewMaps()
		{
			IntVec3 targetMapSize;
			if (Find.Maps.Count != 0)
			{
				targetMapSize = Find.Maps.Select(m => m.Size).MaxBy(m => m.x);
				targetMapSize.y = 1;
				targetMapSize.x = Maths.Max(targetMapSize.x, 125);
				targetMapSize.z = Maths.Max(targetMapSize.z, 125);
			}
			else
			{
				targetMapSize = new IntVec3(125, 1, 125);
			}
			int i = Find.Maps.Count;
			while (i++ < Finder.Settings.Debug_ArenaMaxMapNum)
			{
				MapParent mapParent = (MapParent)WorldObjectMaker.MakeWorldObject(CombatAI_WorldObjectDefOf.CAI_Debug_BattleGround);
				mapParent.Tile = TileFinder.RandomSettlementTileFor(Faction.OfPlayer, mustBeAutoChoosable: true, (int tile) => Find.World.tileTemperatures.GetSeasonalTemp(tile) > 5f);
				mapParent.SetFaction(Faction.OfPlayer);
				Find.WorldObjects.Add(mapParent);
				Map map = GetOrGenerateMapUtility.GetOrGenerateMap(mapParent.Tile, targetMapSize, null);
				map.GetComp_Fast<MapBattleRoyale>().enabled = true;
			}
		}
	}
}

