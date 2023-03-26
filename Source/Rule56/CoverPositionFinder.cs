using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static CombatAI.AvoidanceTracker;
using static CombatAI.SightTracker;

namespace CombatAI
{
    [StaticConstructorOnStartup]
    public static class CoverPositionFinder
    {
	    private static          int         checks             = 0;
	    private static          int         checksSkipped      = 0;
//	    private static          int         checksFault        = 0;
        private static readonly CellMetrics metric_cover       = new CellMetrics();
        private static readonly CellMetrics metric_coverPath   = new CellMetrics();
        private static readonly CellMetrics metric_retreat     = new CellMetrics();
        private static readonly CellMetrics metric_retreatPath = new CellMetrics();
        private static readonly CellMetrics metric_duck        = new CellMetrics();
        private static readonly CellMetrics metric_duckPath    = new CellMetrics();

        private static readonly List<Func<IntVec3, bool>>    enemyVerbs = new List<Func<IntVec3, bool>>();
        private static readonly Dictionary<IntVec3, IntVec3> parentTree = new Dictionary<IntVec3, IntVec3>(512);
        private static readonly Dictionary<IntVec3, float>   scores     = new Dictionary<IntVec3, float>(512);

        static CoverPositionFinder()
        {
            // covering?

            metric_cover.Add("visibilityEnemies", (reader, cell) => reader.GetVisibilityToEnemies(cell), 0.25f);
            metric_cover.Add("threat", (reader,            cell) => reader.GetThreat(cell), 0.25f);

            metric_coverPath.Add("visibilityEnemies", (reader,    cell) => reader.GetVisibilityToEnemies(cell), 4);
            metric_coverPath.Add("traverse", (map,                cell) => (cell.GetEdifice(map)?.def.pathCost / 22f ?? 0) + (cell.GetTerrain(map)?.pathCost / 22f ?? 0), 1, false);
            metric_coverPath.Add("visibilityFriendlies", (reader, cell) => reader.GetVisibilityToFriendlies(cell), -0.05f);

            // retreating

            metric_retreat.Add("visibilityEnemies", (reader,    cell) => reader.GetVisibilityToEnemies(cell));
            metric_retreat.Add("threat", (reader,               cell) => reader.GetThreat(cell), 0.25f);
            metric_retreat.Add("visibilityFriendlies", (reader, cell) => reader.GetVisibilityToFriendlies(cell), -0.10f);

            metric_retreatPath.Add("visibilityEnemies", (reader,    cell) => reader.GetVisibilityToEnemies(cell), 4);
            metric_retreatPath.Add("traverse", (map,                cell) => (cell.GetEdifice(map)?.def.pathCost / 22f ?? 0) + (cell.GetTerrain(map)?.pathCost / 22f ?? 0), 1, false);
            metric_retreatPath.Add("visibilityFriendlies", (reader, cell) => reader.GetVisibilityToFriendlies(cell), -0.05f);
            metric_retreatPath.Add("danger", (reader,               cell) => reader.GetDanger(cell), 0.05f);

            // ducking

            metric_duck.Add("visibilityEnemies", (reader, cell) => reader.GetVisibilityToEnemies(cell), 0.25f);
            metric_duck.Add("threat", (reader,            cell) => reader.GetThreat(cell), 0.25f);

            metric_duckPath.Add("visibilityEnemies", (reader,    cell) => reader.GetVisibilityToEnemies(cell), 4);
            metric_duckPath.Add("traverse", (map,                cell) => (cell.GetEdifice(map)?.def.pathCost / 22f ?? 0) + (cell.GetTerrain(map)?.pathCost / 22f ?? 0), 1, false);
            metric_duckPath.Add("visibilityFriendlies", (reader, cell) => reader.GetVisibilityToFriendlies(cell), -0.05f);
        }

        public static bool TryFindCoverPosition(CoverPositionRequest request, out IntVec3 coverCell, Action<IntVec3, float> callback = null)
        {
	        checks        = 0;
//	        checksFault   = 0;
	        checksSkipped = 0;
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
            metric_cover.Begin(map, sightReader, avoidanceReader, request.locus);
            metric_coverPath.Begin(map, sightReader, avoidanceReader, request.locus);
            IntVec3            dutyDest           = caster.TryGetNextDutyDest(request.maxRangeFromCaster);
            InterceptorTracker interceptors       = map.GetComp_Fast<MapComponent_CombatAI>().interceptors;
            float              maxDistSqr         = request.maxRangeFromLocus * request.maxRangeFromLocus;
            CellFlooder        flooder            = map.GetCellFlooder();
            IntVec3            enemyLoc           = request.target.Cell;
            IntVec3            bestCell           = IntVec3.Invalid;
            float              bestCellVisibility = 1e8f;
            float              bestCellScore      = 1e8f;
            float              effectiveRange     = request.verb != null && request.verb.EffectiveRange > 0 ? request.verb.EffectiveRange * 0.8f : -1;
            float              rootDutyDestDist   = dutyDest.IsValid ? dutyDest.DistanceTo(caster.Position) : -1;
            flooder.Flood(request.locus,
                          node =>
                          {
	                          
                              if (request.verb != null && (sightReader.GetNearestEnemy(node.cell).DistanceToSquared(enemyLoc) > Maths.Sqr(effectiveRange) || !request.verb.CanHitTargetFrom(node.cell, enemyLoc)) || maxDistSqr < request.locus.DistanceToSquared(node.cell) || !map.reservationManager.CanReserve(caster, node.cell))
                              {
                                  return;
                              }
                              float c = (node.dist - node.distAbs) / (node.distAbs + 1f) * 2 - interceptors.grid.Get(node.cell) * 2 + metric_cover.Score(node.cell);
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
                              if (c > 5000)
                              {
                                  Log.Message($"cell {node.cell} has exploding val {c} {Maths.Sqrt_Fast(dutyDest.DistanceToSquared(node.cell), 3)} {Maths.Sqrt_Fast(node.cell.DistanceToSquared(enemyLoc), 3)}, ");
                                  metric_cover.Print(node.cell);
                              }
                          },
                          cell =>
                          {
                              float c = metric_coverPath.Score(cell);
                              if (c > 5000)
                              {
                                  Log.Message($"cell path {cell} has exploding val {c}");
                                  metric_coverPath.Print(cell);
                              }
                              return c - interceptors.grid.Get(cell);
                          },
                          cell =>
                          {
                              return (request.validator == null || request.validator(cell)) && cell.WalkableBy(map, caster);
                          },
                          (int)Maths.Min(request.maxRangeFromLocus, 30)
            );
            coverCell = bestCell;
//            Log.Message($"{checksSkipped}/{checks + checksSkipped}:{checksFault}");
            return bestCell.IsValid;
        }

        public static bool TryFindRetreatPosition(CoverPositionRequest request, out IntVec3 coverCell, Action<IntVec3, float> callback = null)
        {
	        checks        = 0;
//	        checksFault   = 0;
	        checksSkipped = 0;
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
            metric_retreat.Begin(map, sightReader, avoidanceReader, request.locus);
            metric_retreatPath.Begin(map, sightReader, avoidanceReader, request.locus);
            IntVec3            dutyDest           = caster.TryGetNextDutyDest(request.maxRangeFromCaster);
            float              rootDutyDestDist   = dutyDest.IsValid ? dutyDest.DistanceTo(request.locus) : -1;
            IntVec3            enemyLoc           = request.target.Cell;
            InterceptorTracker interceptors       = map.GetComp_Fast<MapComponent_CombatAI>().interceptors;
            float              adjustedMaxDist    = request.maxRangeFromLocus * 2;
            float              adjustedMaxDistSqr = adjustedMaxDist * adjustedMaxDist;
            CellFlooder        flooder            = map.GetCellFlooder();
            IntVec3            bestCell           = IntVec3.Invalid;
            float              bestCellDist       = 0;
            float              bestCellScore      = 1e8f;
            flooder.Flood(request.locus,
                          node =>
                          {
                              parentTree[node.cell] = node.parent;

                              if (adjustedMaxDistSqr < request.locus.DistanceToSquared(node.cell) || !map.reservationManager.CanReserve(caster, node.cell))
                              {
                                  return;
                              }
                              // do math
                              float c = (node.dist - node.distAbs) / (node.distAbs + 1f) - interceptors.grid.Get(node.cell) + metric_retreat.Score(node.cell);
                              // check for blocked line of sight with major threats.
                              if (node.cell == request.locus)
                              {
                                  c += enemiesWarmingUp / 10f;
                              }
                              float d = node.cell.DistanceToSquared(enemyLoc);
                              if (bestCellScore - c >= 0.05f)
                              {
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
                              float cost = metric_retreatPath.Score(cell) - interceptors.grid.Get(cell);
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
//            Log.Message($"{checksSkipped}/{checks + checksSkipped}:{checksFault}");
            return bestCell.IsValid;
        }

        public static bool TryFindDuckPosition(CoverPositionRequest request, out IntVec3 coverCell, Action<IntVec3, float> callback = null)
        {
	        checks        = 0;
//	        checksFault   = 0;
	        checksSkipped = 0;
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
            metric_duck.Begin(map, sightReader, avoidanceReader, request.locus);
            metric_duckPath.Begin(map, sightReader, avoidanceReader, request.locus);
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
                              float c = (node.dist - node.distAbs) / (node.distAbs + 1f) - interceptors.grid.Get(node.cell) * 2 + metric_duck.Score(node.cell);
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
                              return metric_duckPath.Score(cell) - interceptors.grid.Get(cell);
                          },
                          cell =>
                          {
                              return (request.validator == null || request.validator(cell)) && cell.WalkableBy(map, caster);
                          },
                          (int)Maths.Min(request.maxRangeFromLocus, 30)
            );
            coverCell = bestCell;
//            Log.Message($"{checksSkipped}/{checks + checksSkipped}:{checksFault}");
            return bestCell.IsValid && bestVisibleTo == 0;
        }

        private static Func<IntVec3, bool> GetCanHitTargetFunc(Thing thing, Verb verb)
        {
	        IntVec3 position = thing.Position;
	        if (thing.Map.Sight().TryGetReader(thing, out SightReader reader))
	        {
		        ulong thingFlags = thing.GetThingFlags();
		        float minDistSqr = Maths.Min(reader.GetNearestEnemy(position).DistanceToSquared(position), Maths.Sqr(verb.EffectiveRange + 5));
		        return cell =>
		        {
			        if (cell.DistanceToSquared(position) < minDistSqr && (reader.GetDynamicFriendlyFlags(cell) & thingFlags) != 0)
			        {
				        checks++;
				        return verb.CanHitTarget(cell);
			        }
			        checksSkipped++;
//			        if (verb.CanHitTarget(cell))
//			        {
//				        checksFault++;
//			        }
			        return false;
		        };
	        }
	        else
	        {
		        float minDistSqr = Maths.Sqr(verb.EffectiveRange);
		        return cell =>
		        {
			        if (cell.DistanceToSquared(position) < minDistSqr)
			        {
				        checks++;
				        return verb.CanHitTarget(cell);
			        }
			        checksSkipped++;
//			        if (verb.CanHitTarget(cell))
//			        {
//				        checksFault++;
//			        }
			        return false;
		        };
	        }
        }
    }
}
