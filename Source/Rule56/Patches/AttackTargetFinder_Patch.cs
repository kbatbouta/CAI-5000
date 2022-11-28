using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;

namespace CombatAI.Patches
{
    internal static class AttackTargetFinder_Patch
    {
        private static Map map;        
        private static Pawn searcherPawn;
        private static Faction searcherFaction;
		private static Verb searcherVerb;
		private static float dmg05;
		private static ProjectileProperties projectile;
        private static SightTracker.SightReader sightReader;

        [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget))]
        internal static class AttackTargetFinder_BestAttackTarget_Patch
        {
            internal static void Prefix(IAttackTargetSearcher searcher)
            {                
                map = searcher.Thing?.Map;

                if (searcher.Thing is Pawn pawn && !pawn.RaceProps.Animal)
                {
					searcherPawn = pawn;
                    searcherVerb = pawn.CurrentEffectiveVerb;
                    if(searcherVerb != null && !searcherVerb.IsMeleeAttack && (projectile = searcherVerb.GetProjectile()?.projectile ?? null) != null)
                    {                        
                        dmg05 = projectile.damageAmountBase / 2f;
					}
					pawn.GetSightReader(out sightReader);
                }
                searcherFaction = searcher.Thing?.Faction ?? null;
            }

            internal static void Postfix()
            {
                map = null;
				searcherPawn = null;
				sightReader = null;                
            }
        }

        [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.GetShootingTargetScore))]
        internal static class Harmony_AttackTargetFinder_GetShootingTargetScore
        {
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool finished = false;
                for (int i = 0; i < instructions.Count(); i++)
                {
                    if (!finished)
                    {
                        if (codes[i].opcode == OpCodes.Ldc_R4)
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldarg_1);
                            yield return new CodeInstruction(OpCodes.Ldarg_2);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_AttackTargetFinder_GetShootingTargetScore), nameof(Harmony_AttackTargetFinder_GetShootingTargetScore.GetTargetBaseScore)));
                            continue;
                        }
                    }
                    yield return codes[i];
                }
            }

            public static float GetTargetBaseScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
            {
                float result = 60f;
                if (sightReader != null && searcherPawn != null)
                {                    
                    if ((verb.IsMeleeAttack || verb.EffectiveRange <= 15))
                    {
                        if (sightReader.GetAbsVisibilityToEnemies(target.Thing.Position) > sightReader.GetAbsVisibilityToFriendlies(target.Thing.Position) + 1)
                        {
                            result -= 30f * Finder.P50;
                        }
                        if (sightReader.GetVisibilityToEnemies(target.Thing.Position) > 3)
                        {
                            result -= 15f * Finder.P50;
                        }
                        result += sightReader.GetEnemyDirection(target.Thing.Position).sqrMagnitude - Mathf.Pow(sightReader.GetVisibilityToEnemies(target.Thing.Position), 2);                      
                    }
                    if(target.Thing is Pawn enemy)
                    {
                        if (projectile != null)
                        {
							var report = ArmorUtility.GetArmorReport(enemy);
                            var armor = report.GetArmor(projectile.damageDef);
                            if (projectile.armorPenetrationBase > 0)
                            {
								result += Mathf.Lerp(0f, 12f, projectile.armorPenetrationBase / (armor + 1e-2f));
                                //if (Find.Selector.SelectedPawns.Contains(searcher.Thing as Pawn))
                                //{
                                //    map.debugDrawer.FlashCell(searcher.Thing.Position, projectile.armorPenetrationBase, $"{projectile.armorPenetrationBase}");
                                //    map.debugDrawer.FlashCell(target.Thing.Position, projectile.armorPenetrationBase, $"{Mathf.Lerp(0, dmg05, projectile.armorPenetrationBase / (armor + 1e-2f))}");
                                //}
                            }
                            else
                            { 
								result -= Maths.Min(armor * 0.5f, 8f);
							}
						}                                                
						Thing targeted;
                        if (enemy.stances?.stagger != null && enemy.stances.stagger.Staggered)
                        {
                            result += 12;
						}                        
                        if (!verb.IsMeleeAttack)
                        {
                            if ((enemy.CurrentEffectiveVerb?.IsMeleeAttack ?? false))
                            {
                                if (searcherFaction != null && (targeted = enemy.jobs?.curJob?.targetA.Thing)?.Faction == searcherFaction)
                                {
                                    result += 8 + enemy.Position.DistanceToSquared(targeted.Position) < 150 ? 16 : 0;
                                }
                            }
							if (enemy.pather?.MovingNow ?? false)
							{
								result += 16;
							}                            
						}
						if (!enemy.HasWeaponVisible())
						{
                            result -= 8;
						}                                           
					}
                }
                return result;
            }
        }
    }
}

