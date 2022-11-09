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
    public static class AttackTargetCache_Patch
    {
        //[HarmonyPatch(typeof(AttackTargetsCache), nameof(AttackTargetsCache.GetPotentialTargetsFor))]
        //static class AttackTargetCache_GetPotentialTargetsFor_Patch
        //{
        //    public static void Postfix(IAttackTargetSearcher th, List<IAttackTarget> __result)
        //    {
        //        if(th.Thing is Pawn pawn)
        //        {
        //            pawn.GetSightReader(out SightTracker.SightReader reader);
        //            if ((th.CurrentEffectiveVerb?.IsMeleeAttack ?? false) || (th.CurrentEffectiveVerb?.EffectiveRange < 15))
        //            {                        
        //                __result.RemoveAll(t =>
        //                {
        //                    IntVec3 pos = t.Thing.Position;
        //                    return reader.GetAbsVisibilityToEnemies(pos) > reader.GetAbsVisibilityToFriendlies(pos) + 1 || reader.GetVisibilityToEnemies(pos) > 3;
        //                });
        //            }
        //        }
        //    }
        //}      
    }
}

