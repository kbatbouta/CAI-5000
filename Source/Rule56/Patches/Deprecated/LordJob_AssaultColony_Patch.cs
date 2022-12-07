using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Linq;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;
using static CombatAI.SightTracker;
using static CombatAI.CellFlooder;
using System.Reflection.Emit;
using System.Reflection;

namespace CombatAI.Patches
{
	//public static class LordJob_AssaultColony_Patch
	//{
	//    [HarmonyPatch]
	//    static class LordJob_AssaultColony_Constructor_Patch
	//    {
	//        public static MethodBase TargetMethod()
	//        {
	//            return AccessTools.Constructor(typeof(LordJob_AssaultColony), parameters: new[] {typeof(Faction), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) } );
	//        }

	//        public static void Prefix(Faction assaulterFaction, ref bool canKidnap, ref bool canTimeoutOrFlee, ref bool sappers, ref bool useAvoidGridSmart, ref bool canSteal, ref bool breachers, ref bool canPickUpOpportunisticWeapons)
	//        {
	//            //if (assaulterFaction?.def == FactionDefOf.Mechanoid || assaulterFaction?.def == FactionDefOf.Insect)
	//            //{
	//            //    return;
	//            //}
	//            //sappers = true;
	//            //breachers = true;
	//            //canKidnap = true;
	//            //canPickUpOpportunisticWeapons = true;
	//        }
	//    }            
	//}
}
