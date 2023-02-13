using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static CombatAI.AvoidanceTracker;
using static CombatAI.SightTracker;

namespace CombatAI
{
	public static class CoverPositionFinder
	{
		private static readonly List<Func<IntVec3, bool>>    enemyVerbs = new List<Func<IntVec3, bool>>();
		private static readonly Dictionary<IntVec3, IntVec3> parentTree = new Dictionary<IntVec3, IntVec3>(512);
		private static readonly Dictionary<IntVec3, float>   scores     = new Dictionary<IntVec3, float>(512);

		public static bool TryFindCoverPosition(CoverPositionRequest request, out IntVec3 coverCell, Action<IntVec3, float> callback = null)
		{
			request.caster.TryGetSightReader(out SightReader sightReader);
			request.caster.TryGetAvoidanceReader(out AvoidanceReader avoidanceReader);
			if (sightReader == null || avoidanceReader == null)
			{
				coverCell = IntVec3.Invalid;
				return false;
			}
			Map  map    = request.caster.Map;
			Pawn caster = request.caster;
			sightReader.armor = caster.GetArmorReport();
			if (request.locus == IntVec3.Zero)
			{
				request.locus             = request.caster.Position;
				request.maxRangeFromLocus = request.maxRangeFromCaster;
			}
			if (request.maxRangeFromLocus == 0)
			{
				request.maxRangeFromLocus = float.MaxValue;
			}
			enemyVerbs.Clear();
			int enemiesWarmingUp = 0;
			if (request.majorThreats != null)
			{
				for (int i = 0; i < request.majorThreats.Count; i++)
				{
					Verb verb = request.majorThreats[i].TryGetAttackVerb();
					if (verb != null && !verb.IsMeleeAttack)
					{
						enemyVerbs.Add(GetCanHitTargetFunc(request.majorThreats[i], verb));
						if (verb.WarmingUp || verb.Bursting)
						{
							enemiesWarmingUp += 1;
						}
					}
				}
			}
			IntVec3            dutyDest                = caster.TryGetNextDutyDest(request.maxRangeFromCaster);
			InterceptorTracker interceptors            = map.GetComp_Fast<MapComponent_CombatAI>().interceptors;
			float              maxDistSqr              = request.maxRangeFromLocus * request.maxRangeFromLocus;
			CellFlooder        flooder                 = map.GetCellFlooder();
			IntVec3            enemyLoc                = request.target.Cell;
			IntVec3            bestCell                = IntVec3.Invalid;
			float              rootVis                 = sightReader.GetVisibilityToEnemies(request.locus);
			float              rootThreat              = sightReader.GetThreat(request.locus);
			float              bestCellVisibility      = 1e8f;
			float              bestCellScore           = 1e8f;
			float              effectiveRange          = request.verb != null && request.verb.EffectiveRange > 0 ? request.verb.EffectiveRange * 0.8f : -1;
			float              rootDutyDestDist        = dutyDest.IsValid ? dutyDest.DistanceTo(caster.Position) : -1;
			flooder.Flood(request.locus,
			              node =>
			              {
				              if (request.verb != null && !request.verb.CanHitTargetFrom(node.cell, enemyLoc) || maxDistSqr < request.locus.DistanceToSquared(node.cell) || !map.reservationManager.CanReserve(caster, node.cell))
				              {
					              return;
				              }
				              float c = (node.dist - node.distAbs) / (node.distAbs + 1f) * 2 - interceptors.grid.Get(node.cell) * 2 + (sightReader.GetThreat(node.cell) - rootThreat) * 0.25f;
				              if (node.cell == request.locus)
				              {
					              c += enemiesWarmingUp / 10f;
				              }
				              if (rootDutyDestDist > 0)
				              {
					              c += Mathf.Clamp((Maths.Sqrt_Fast(dutyDest.DistanceToSquared(node.cell), 3) - rootDutyDestDist) * 0.25f, -1f, 1f);
				              }
				              if (effectiveRange > 0)
				              {
					              c += 2f * Mathf.Abs(effectiveRange - Maths.Sqrt_Fast(node.cell.DistanceToSquared(enemyLoc), 5)) / effectiveRange;
				              }
				              if (enemyVerbs.Count > 0)
				              {
					              for (int i = 0; i < enemyVerbs.Count; i++)
					              {
						              if (enemyVerbs[i](node.cell))
						              {
							              c += 1;
						              }
					              }
				              }
				              if (bestCellScore - c >= 0.05f)
				              {
					              float v = sightReader.GetVisibilityToEnemies(node.cell);
					              if (v < bestCellVisibility)
					              {
						              bestCellScore      = c;
						              bestCellVisibility = v;
						              bestCell           = node.cell;
					              }
				              }
				              if (callback != null)
				              {
					              callback(node.cell, c);
				              }
			              },
			              cell =>
			              {
				              return (cell.GetEdifice(map)?.def.pathCost / 22f ?? 0) + (cell.GetTerrain(map)?.pathCost / 22f ?? 0) + (sightReader.GetVisibilityToEnemies(cell) - rootVis) * 2 - interceptors.grid.Get(cell);
			              },
			              cell =>
			              {
				              return (request.validator == null || request.validator(cell)) && cell.WalkableBy(map, caster);
			              },
			              (int)Maths.Min(request.maxRangeFromLocus, 30)
			);
			coverCell = bestCell;
			return bestCell.IsValid;
		}

		public static bool TryFindRetreatPosition(CoverPositionRequest request, out IntVec3 coverCell, Action<IntVec3, float> callback = null)
		{
			request.caster.TryGetSightReader(out SightReader sightReader);
			request.caster.TryGetAvoidanceReader(out AvoidanceReader avoidanceReader);
			if (sightReader == null || avoidanceReader == null)
			{
				coverCell = IntVec3.Invalid;
				return false;
			}
			Map  map    = request.caster.Map;
			Pawn caster = request.caster;
			sightReader.armor = caster.GetArmorReport();
			if (request.locus == IntVec3.Zero)
			{
				request.locus             = request.caster.Position;
				request.maxRangeFromLocus = request.maxRangeFromCaster;
			}
			if (request.maxRangeFromLocus == 0)
			{
				request.maxRangeFromLocus = float.MaxValue;
			}
			int enemiesWarmingUp = 0;
			if (request.majorThreats != null)
			{
				for (int i = 0; i < request.majorThreats.Count; i++)
				{
					Verb verb = request.majorThreats[i].TryGetAttackVerb();
					if (verb != null && !verb.IsMeleeAttack)
					{
						enemyVerbs.Add(GetCanHitTargetFunc(request.majorThreats[i], verb));
						if (verb.WarmingUp || verb.Bursting)
						{
							enemiesWarmingUp += 1;
						}
					}
				}
			}
			parentTree.Clear();
			scores.Clear();
			IntVec3            dutyDest                = caster.TryGetNextDutyDest(request.maxRangeFromCaster);
			float              rootDutyDestDist        = dutyDest.IsValid ? dutyDest.DistanceTo(request.locus) : -1;
			IntVec3            enemyLoc                = request.target.Cell;
			InterceptorTracker interceptors            = map.GetComp_Fast<MapComponent_CombatAI>().interceptors;
			CellIndices        indices                 = map.cellIndices;
			float              adjustedMaxDist         = request.maxRangeFromLocus * 2;
			float              adjustedMaxDistSqr      = adjustedMaxDist * adjustedMaxDist;
			CellFlooder        flooder                 = map.GetCellFlooder();
			IntVec3            bestCell                = IntVec3.Invalid;
			float              rootVis                 = sightReader.GetVisibilityToEnemies(request.locus);
			float              rootVisFriendlies       = sightReader.GetVisibilityToFriendlies(request.locus);
			float              rootThreat              = sightReader.GetThreat(request.locus);
			float              bestCellDist            = request.locus.DistanceToSquared(enemyLoc);
			float              bestCellScore           = 1e8f;
			flooder.Flood(request.locus,
			              node =>
			              {
				              parentTree[node.cell] = node.parent;

				              if (adjustedMaxDistSqr < request.locus.DistanceToSquared(node.cell) || !map.reservationManager.CanReserve(caster, node.cell))
				              {
					              return;
				              }
				              // do math
				              float c = (node.dist - node.distAbs) / (node.distAbs + 1f) * 2 + avoidanceReader.GetProximity(node.cell) * 0.5f - interceptors.grid.Get(node.cell) + (sightReader.GetThreat(node.cell) - rootThreat) * 0.75f;
				              // check for blocked line of sight with major threats.
				              if (node.cell == request.locus)
				              {
					              c += enemiesWarmingUp / 10f;
				              }
				              if (rootDutyDestDist > 0)
				              {
					              c += Mathf.Clamp((Maths.Sqrt_Fast(dutyDest.DistanceToSquared(node.cell), 5) - rootDutyDestDist) * 0.25f, -1f, 1f);
				              }
				              if (bestCellScore - c >= 0.05f)
				              {
					              float d = node.cell.DistanceToSquared(enemyLoc);
					              if (d > bestCellDist)
					              {
						              bestCellScore = c;
						              bestCellDist  = d;
						              bestCell      = node.cell;
					              }
				              }
				              if (callback != null)
				              {
					              callback(node.cell, c);
				              }
				              scores[node.cell] = c;
			              },
			              cell =>
			              {
				              float cost = (cell.GetEdifice(map)?.def.pathCost / 22f ?? 0) + (cell.GetTerrain(map)?.pathCost / 22f ?? 0) + (sightReader.GetVisibilityToEnemies(cell) - rootVis) * 2 - (rootVisFriendlies - sightReader.GetVisibilityToFriendlies(cell)) - interceptors.grid.Get(cell) + (sightReader.GetThreat(cell) - rootThreat) * 0.25f;
				              if (enemyVerbs.Count > 0)
				              {
					              for (int i = 0; i < enemyVerbs.Count; i++)
					              {
						              if (enemyVerbs[i](cell))
						              {
							              cost += 1.0f;
						              }
					              }
				              }
				              return cost;
			              },
			              cell =>
			              {
				              return (request.validator == null || request.validator(cell)) && cell.WalkableBy(map, caster);
			              },
			              (int)Maths.Min(adjustedMaxDist, 45f)
			);
			if (!bestCell.IsValid)
			{
				coverCell = IntVec3.Invalid;
				return false;
			}
			int distSqr = Mathf.CeilToInt(request.maxRangeFromLocus * request.maxRangeFromLocus);
			if (bestCellDist > distSqr)
			{
				IntVec3 cell = bestCell;
				while (parentTree.TryGetValue(cell, out IntVec3 parent) && parent != cell && parent.DistanceToSquared(request.locus) > distSqr)
				{
					cell = parent;
				}
				bestCell = cell;
			}
			coverCell = bestCell;
			return bestCell.IsValid;
		}
		
		public static bool TryFindDuckPosition(CoverPositionRequest request, out IntVec3 coverCell, Action<IntVec3, float> callback = null)
		{
			request.caster.TryGetSightReader(out SightReader sightReader);
			request.caster.TryGetAvoidanceReader(out AvoidanceReader avoidanceReader);
			if (sightReader == null || avoidanceReader == null)
			{
				coverCell = IntVec3.Invalid;
				return false;
			}
			Map  map    = request.caster.Map;
			Pawn caster = request.caster;
			sightReader.armor = caster.GetArmorReport();
			if (request.locus == IntVec3.Zero)
			{
				request.locus             = request.caster.Position;
				request.maxRangeFromLocus = request.maxRangeFromCaster;
			}
			if (request.maxRangeFromLocus == 0)
			{
				request.maxRangeFromLocus = float.MaxValue;
			}
			enemyVerbs.Clear();
			int enemiesWarmingUp = 0;
			if (request.majorThreats != null)
			{
				for (int i = 0; i < request.majorThreats.Count; i++)
				{
					Verb verb = request.majorThreats[i].TryGetAttackVerb();
					if (verb != null && !verb.IsMeleeAttack)
					{
						enemyVerbs.Add(GetCanHitTargetFunc(request.majorThreats[i], verb));
						if (verb.WarmingUp || verb.Bursting)
						{
							enemiesWarmingUp += 1;
						}
					}
				}
			}
			if (enemyVerbs.Count == 0)
			{
				Log.Warning("TryFindDuckPosition got no major threats nor a target thing. Locs num is 0.");
				coverCell = IntVec3.Invalid;
				return false;
			}
			IntVec3            dutyDest         = caster.TryGetNextDutyDest(request.maxRangeFromCaster);
			float              rootDutyDestDist = dutyDest.IsValid ? dutyDest.DistanceTo(request.locus) : -1;
			InterceptorTracker interceptors     = map.GetComp_Fast<MapComponent_CombatAI>().interceptors;
			float              maxDistSqr       = request.maxRangeFromLocus * request.maxRangeFromLocus;
			CellFlooder        flooder          = map.GetCellFlooder();
			IntVec3            bestCell         = IntVec3.Invalid;
			float              bestCellScore    = 1e8f;
			float              rootVis          = sightReader.GetVisibilityToEnemies(request.locus);
			float              rootThreat       = sightReader.GetThreat(request.locus);
			int                bestVisibleTo    = 256;
			flooder.Flood(request.locus,
			              node =>
			              {
				              if (maxDistSqr < request.locus.DistanceToSquared(node.cell) || !map.reservationManager.CanReserve(caster, node.cell))
				              {
					              return;
				              }
				              float c = (node.dist - node.distAbs) / (node.distAbs + 1f) - interceptors.grid.Get(node.cell) * 2 + (sightReader.GetThreat(node.cell) - rootThreat) * 0.1f;
				              // check for blocked line of sight with major threats.
				              int visibleTo = 0;
				              for (int i = 0; i < enemyVerbs.Count; i++)
				              {
					              if (enemyVerbs[i](node.cell))
					              {
						              c += 1;
						              visibleTo++;
					              }
				              }
				              if (node.cell == request.locus)
				              {
					              c += enemiesWarmingUp / 5f;
				              }
				              if (rootDutyDestDist > 0)
				              {
					              c += Mathf.Clamp((Maths.Sqrt_Fast(dutyDest.DistanceToSquared(node.cell), 5) - rootDutyDestDist) * 0.25f, -1f, 1f);
				              }
				              if (bestCellScore - c >= 0.05f)
				              {
					              if (visibleTo <= bestVisibleTo)
					              {
						              bestCellScore = c;
						              bestCell      = node.cell;
						              bestVisibleTo = visibleTo;
					              }
				              }
				              if (callback != null)
				              {
					              callback(node.cell, c);
				              }
			              },
			              cell =>
			              {
				              return (cell.GetEdifice(map)?.def.pathCost / 22f ?? 0) + (cell.GetTerrain(map)?.pathCost / 22f ?? 0) + (sightReader.GetVisibilityToEnemies(cell) - rootVis) - interceptors.grid.Get(cell);
			              },
			              cell =>
			              {
				              return (request.validator == null || request.validator(cell)) && cell.WalkableBy(map, caster);
			              },
			              (int)Maths.Min(request.maxRangeFromLocus, 30)
			);
			coverCell = bestCell;
			return bestCell.IsValid && (bestVisibleTo <= request.majorThreats.Count / 3f || bestVisibleTo == 0);
		}

		private static Func<IntVec3, bool> GetCanHitTargetFunc(Thing thing, Verb enemyVerb)
		{
			return cell => enemyVerb.CanHitTarget(cell);
		}
	}
}
