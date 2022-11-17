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
        private static SightTracker.SightReader sightReader;
       
        [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget))]
        internal static class AttackTargetFinder_BestAttackTarget_Patch
        {           
            internal static void Prefix(IAttackTargetSearcher searcher)
            {                
                map = searcher.Thing?.Map;

                if (searcher.Thing is Pawn pawn)
                {
                    pawn.GetSightReader(out sightReader);
                }
            }

            internal static void Postfix()
            {
                map = null;
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
                if (verb.IsMeleeAttack || verb.EffectiveRange <= 15)
                {
                    if (sightReader != null)
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
                    //if (searcher.Thing is Pawn pawn && Find.Selector.SelectedPawns.Contains(pawn))
                    //{
                    //    map.debugDrawer.FlashCell(target.Thing.Position, 1, $"{result}");
                    //}
                }               
                return result;
            }
        }
    }
}

