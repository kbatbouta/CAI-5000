using System;
using System.Collections;
using System.Collections.Generic;
using CombatAI.Comps;
using NAudio.SoundFont;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CombatAI
{
	public class MapBattleRoyale : MapComponent
	{
		private enum PawnState
		{
			dead = 0,
			alive = 1,			
		}

		public bool enabled;
		public IntVec3 lhsSpawnPoint;
		public IntVec3 rhsSpawnPoint;
		public int roundTicksLeft;
		public int roundsLeft;
		public int lhsStartNum;
		public int rhsStartNum;		
		public List<Pawn> lhs = new List<Pawn>();
		public List<Pawn> rhs = new List<Pawn>();		
		public BattleRoyaleParms parms;
		public HashSet<Pawn> lhSet = new HashSet<Pawn>();
		public HashSet<Pawn> rhSet = new HashSet<Pawn>();		
		private List<Lord> battleLords = new List<Lord>();		

		public MapBattleRoyale(Map map) : base(map)
		{
		}

		public bool Active
		{
			get => parms.IsValid && BattleRoyale.enabled;
		}

		public override void MapComponentTick()
		{
			base.MapComponentTick();			
			if (!BattleRoyale.manager.Active)
			{
				return;
			}
			if (!parms.IsValid)
			{			
				return;
			}
			roundTicksLeft--;
			if (roundTicksLeft <= 0)
			{
				TryEndBattle(true);
				return;
			}
			if (GenTicks.TicksGame % 60 != 0)
			{
				return;
			}
			map.debugDrawer.FlashCell(map.Center, 1.0f, "mc", 60);
			int i;
			i = lhs.Count - 1;
			while (i >= 0)
			{
				Pawn pawn = lhs[i];
				PawnState state = GetPawnState(pawn);
				switch (state)
				{
					case PawnState.dead:
						TryRemovePawn(pawn, true);
						BattleRoyale.lhsPawns.Remove(pawn);						
						lhs.RemoveAt(i);
						lhSet.Remove(pawn);
						break;
					case PawnState.alive:
						UpdatePawnDuty(pawn);
						break;
				}
				i--;
			}
			i = rhs.Count - 1;
			while (i >= 0)
			{
				Pawn pawn = rhs[i];
				PawnState state = GetPawnState(pawn);
				switch (state)
				{
					case PawnState.dead:
						TryRemovePawn(pawn, false);
						BattleRoyale.rhsPawns.Remove(pawn);
						rhs.RemoveAt(i);
						rhSet.Remove(pawn);
						break;
					case PawnState.alive:
						UpdatePawnDuty(pawn);				
						break;
				}
				i--;
			}
			TryEndBattle(false);
		}

		public void TryEndBattle(bool endNow)
		{
			bool lhsEmpty = lhs.Count == 0;
			bool rhsEmpty = rhs.Count == 0;
			BattleResult result = BattleResult.inprogress;
			if ((lhsEmpty || rhsEmpty))
			{				
				if (!lhsEmpty && rhsEmpty)				
					result = BattleResult.lhs_winner;				
				else if (!rhsEmpty && lhsEmpty)		
					result = BattleResult.rhs_winner;				
				else				
					result = BattleResult.stalemate;				
				endNow |= true;
			}
			if (endNow && result == BattleResult.inprogress)
			{
				float lr = (float)lhs.Count / lhsStartNum;
				float rr = (float)rhs.Count / rhsStartNum;
				if (Mathf.Abs(lr - rr) > 0.05)
				{
					if (lr > rr)
						result = BattleResult.lhs_winner;
					else
						result = BattleResult.rhs_winner;
				}
				else
				{
					result = BattleResult.stalemate;
				}
			}
			if(result != BattleResult.inprogress)
			{
				float lr = (float)lhs.Count / lhsStartNum;
				float rr = (float)rhs.Count / rhsStartNum;
				parms.callback(result, lhs, rhs, lr, rr);
				Log.Message($"Battle ended with lr:{(float)lhs.Count / lhsStartNum}, rr:{(float)rhs.Count / rhsStartNum}, result:{result}");
				Restart();
			}
		}

		public void StartBattleRoyale(BattleRoyaleParms parms)
		{			
			this.parms = parms;
			roundsLeft = parms.runs;			
			CleanUp();
			StartBattleRoyalMap();
		}	

		public void Restart()
		{						
			if (roundsLeft > 0)
			{
				CleanUp();
				StartBattleRoyalMap();
			}
			else
			{
				Stop();
			}
		}

		public void Stop()
		{
			CleanUp();
			parms = default(BattleRoyaleParms);
		}			

		public override void MapComponentOnGUI()
		{
			base.MapComponentOnGUI();
			if (!Active || lhsStartNum == 0)
			{
				return;
			}
			Rect r = new Rect(0, 30, 300, 20);
			Widgets.Label(r, $"map battle stats: lr:<color=green>{Math.Round((float)lhs.Count / lhsStartNum, 2)}</color>({parms.lhsAi}), rr:<color=red>{Math.Round((float)rhs.Count / rhsStartNum, 2)}</color>({parms.rhsAi}), time left:<color=yellow>{Mathf.RoundToInt(roundTicksLeft / 60)} seconds</color>");

			if (Input.GetKey(KeyCode.LeftShift))
			{
				for (int i = 0; i < lhs.Count; i++)
				{
					Pawn pawn = lhs[i];
					Vector3 drawPos = pawn.DrawPos;
					drawPos.z += 0.5f;
					Vector2 uiPos = drawPos.MapToUIPosition();
					Color32 color = pawn.GetAIType() != parms.lhsAi ? Color.yellow : Color.green;
					Widgets.DrawBoxSolid(new Rect(uiPos.x - 4, uiPos.y - 4, 8, 8), color);
				}
				for (int i = 0; i < rhs.Count; i++)
				{
					Pawn pawn = rhs[i];
					Vector3 drawPos = pawn.DrawPos;
					drawPos.z += 0.5f;
					Vector2 uiPos = drawPos.MapToUIPosition();
					Color32 color = pawn.GetAIType() != parms.rhsAi ? Color.yellow : Color.red;
					Widgets.DrawBoxSolid(new Rect(uiPos.x - 4, uiPos.y - 4, 8, 8), color);
				}
			}
		}

		private void SpawnPawnSet(List<PawnKindDef> kinds, IntVec3 spot, Faction faction, List<Pawn> result, HashSet<Pawn> resultSet, bool modAI)
		{
			string msg = null;
			for (int i = 0; i < kinds.Count; i++)
			{

				Pawn pawn = PawnGenerator.GeneratePawn(kinds[i], faction);
				if (modAI)
				{
					ThingComp_CombatAI comp = pawn.GetComp_Fast<ThingComp_CombatAI>();
					if (!(pawn.CurrentEffectiveVerb?.IsMeleeAttack ?? false))
					{
						comp.sequential = BattleRoyale.manager.reactionBreeder.TryBreedNewSpecimen(null);
						if (Rand.Chance(1f / kinds.Count) && msg == null)
						{
							msg = "Breeding sample:\n";
							for (int j = 0; j < comp.sequential.weights.Count; j++)
							{
								msg += comp.sequential.weights[j].ToString() + "\n";
							}
						}
					}
					else
					{
						comp.sequential = null;
					}
				}				
				IntVec3 loc = CellFinder.RandomClosewalkCellNear(spot, map, 12);
				GenSpawn.Spawn(pawn, loc, map, Rot4.Random);
				result.Add(pawn);
				resultSet.Add(pawn);				
			}
			if (msg != null)
			{
				Log.Message(msg);
			}

			battleLords.Add(LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(map.Center), map, result));
		}

		private void StartBattleRoyalMap()
		{
			SetupSpawnPoints();
			roundTicksLeft = parms.maxRoundTicks;
			lhSet.Clear();
			rhSet.Clear();
			lhs.Clear();
			rhs.Clear();
			SpawnPawnSet(parms.lhs, lhsSpawnPoint, Faction.OfAncients, lhs, lhSet, true);
			BattleRoyale.lhsPawns.AddRange(lhs);
			SpawnPawnSet(parms.rhs, rhsSpawnPoint, Faction.OfAncientsHostile, rhs, rhSet, false);
			BattleRoyale.rhsPawns.AddRange(rhs);
			lhsStartNum = lhs.Count;
			rhsStartNum = rhs.Count;
			roundsLeft--;
		}

		private void UpdatePawnDuty(Pawn pawn)
		{
			if (pawn.mindState.duty != null && pawn.mindState.duty.def == DutyDefOf.Defend && (pawn.mindState.duty.focus == map.Center || pawn.mindState.duty.focusSecond == map.Center)) 
			{
				return;
			}
			IntVec3 center = map.Center;
			pawn.mindState.duty = new PawnDuty(DutyDefOf.Defend, center);
			pawn.mindState.duty.focusSecond = center;
			pawn.mindState.duty.radius = map.Size.x / 2f;
			pawn.mindState.duty.wanderRadius = 64;
		}

		private void CleanUp()
		{			
			for (int i = 0; i < lhs.Count; i++)
			{
				Pawn pawn = lhs[i];
				BattleRoyale.lhsPawns.Remove(pawn);
				TryRemovePawn(pawn, true);
			}			
			for (int i = 0; i < rhs.Count; i++)
			{
				Pawn pawn = rhs[i];
				BattleRoyale.rhsPawns.Remove(pawn);
				TryRemovePawn(pawn, false);
			}
			for(int i = 0;i < battleLords.Count; i++)
			{
				Lord lord = battleLords[i];
				if (map.lordManager.lords.Any(l => l == lord))
				{
					map.lordManager.RemoveLord(lord);
				}
			}
			battleLords.Clear();
			lhs.Clear();
			rhs.Clear();
			rhSet.Clear();
			lhSet.Clear();
			lhsStartNum = 0;
			rhsStartNum = 0;
			roundTicksLeft = 0;
		}

		private void SetupSpawnPoints()
		{
			MultipleCaravansCellFinder.FindStartingCellsFor2Groups(map, out lhsSpawnPoint, out rhsSpawnPoint);
			switch (parms.spawn)
			{				
				case BattleSpawn.lhsMapCenterAlways:
					lhsSpawnPoint = map.Center;
					break;
				case BattleSpawn.rhsMapCenterAlways:
					rhsSpawnPoint = map.Center;
					break;
				case BattleSpawn.lhsMapCenterAllowed:
					if (Rand.Chance(0.5f))
						lhsSpawnPoint = map.Center;
					break;
				case BattleSpawn.rhsMapCenterAllowed:
					if (Rand.Chance(0.5f))
						rhsSpawnPoint = map.Center;
					break;
				case BattleSpawn.random:
					break;
				case BattleSpawn.randomMapCenterAllowed:
					if (Rand.Chance(0.5f))
						lhsSpawnPoint = map.Center;
					else
						rhsSpawnPoint = map.Center;
					break;
			}
		}

		private PawnState GetPawnState(Pawn pawn)
		{
			if (pawn.Destroyed || !pawn.Spawned || pawn.Dead || pawn.Downed)
			{
				return PawnState.dead;
			}
			return PawnState.alive;
		}

		private void TryRemovePawn(Pawn pawn, bool modAI)
		{
			if(pawn == null)
			{
				return;
			}
			if (modAI)
			{
				ThingComp_CombatAI comp = pawn.GetComp_Fast<ThingComp_CombatAI>();
				if (comp != null && comp.sequential != null && comp.hitsLanded > 0)
				{
					if (!pawn.Dead)
					{
						comp.hitsLanded *= 2;
					}
					BattleRoyale.manager.reactionBreeder.queue.Add(new SeqBreeder.SeqSpecimen()
					{
						score = comp.hitsLanded,
						sequential = comp.sequential
					});
				}
			}
			if (pawn.Dead && pawn.Corpse != null)
			{
				pawn.Corpse.Destroy();
			}
			if (!pawn.Destroyed)
			{
				pawn.Destroy(DestroyMode.Vanish);
			}
		}		
	}	
}

