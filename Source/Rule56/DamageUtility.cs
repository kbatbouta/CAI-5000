using System;
using System.Collections.Generic;
using CombatAI.Gui;
using RimWorld;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public static class DamageUtility
    {
        private static readonly Dictionary<int, DamageReport> reports = new Dictionary<int, DamageReport>(1024);

        public static DamageReport GetDamageReport(Thing thing, Listing_Collapsible collapsible = null)
        {
            if (collapsible == null && reports.TryGetValue(thing.thingIDNumber, out DamageReport report) && report.IsValid)
            {
                return report;
            }
            report = new DamageReport();
            bool debug = collapsible != null;
            report.thing = thing;
            if (thing is Pawn pawn)
            {
                report.canMelee = true;
                if (debug)
                {
                    collapsible.Label($"Pawn {pawn}({pawn.thingIDNumber})[{pawn.Position}]");
                }
                if (pawn.equipment != null)
                {
                    if (debug)
                    {
                        collapsible.Line(2);
                        collapsible.Label("Equipment");
                    }
                    foreach (Verb verb in pawn.equipment.AllEquipmentVerbs)
                    {
                        report.AddVerb(verb);
                        if (debug)
                        {
                            collapsible.Label($"Eq.verb ({verb})<{verb.EquipmentSource},{verb.GetProjectile()}>");
                            collapsible.Label($"r.ranged rS:{Math.Round(report.rangedSharp, 4)}\trB:{Math.Round(report.rangedBlunt, 4)}\tmeta:{report.attributes}");
                            collapsible.Label($"r.ranged rSAp:{Math.Round(report.rangedSharpAp, 4)}\trBAp:{Math.Round(report.rangedBluntAp, 4)}");
                            collapsible.Label($"r.melee mS:{Math.Round(report.meleeSharp, 4)}\tmB:{Math.Round(report.meleeBlunt, 4)}");
                            collapsible.Label($"r.melee mSAp:{Math.Round(report.meleeSharpAp, 4)}\tmBAp:{Math.Round(report.meleeBluntAp, 4)}");
                            collapsible.Gap(2);
                        }
                    }
                }
                if (pawn.meleeVerbs != null)
                {
                    if (debug)
                    {
                        collapsible.Line(2);
                        collapsible.Label("Melee");
                    }
                    List<VerbEntry> verbs = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(false);
                    for (int i = 0; i < verbs.Count; i++)
                    {
                        report.AddVerb(verbs[i].verb);
                        if (debug)
                        {
                            collapsible.Label($"M.verb ({verbs[i].verb})<{verbs[i].verb.EquipmentSource}>");
                            collapsible.Label($"r.ranged rS:{Math.Round(report.rangedSharp, 4)}\trB:{Math.Round(report.rangedBlunt, 4)}\tmeta:{report.attributes}");
                            collapsible.Label($"r.ranged rSAp:{Math.Round(report.rangedSharpAp, 4)}\trBAp:{Math.Round(report.rangedBluntAp, 4)}");
                            collapsible.Label($"r.melee mS:{Math.Round(report.meleeSharp, 4)}\tmB:{Math.Round(report.meleeBlunt, 4)}");
                            collapsible.Label($"r.melee mSAp:{Math.Round(report.meleeSharpAp, 4)}\tmBAp:{Math.Round(report.meleeBluntAp, 4)}");
                        }
                    }
                }
                Verb effectiveVerb = pawn.TryGetAttackVerb();
                if (effectiveVerb != null)
                {
                    if (!effectiveVerb.IsMeleeAttack && !pawn.Downed)
                    {
                        report.primaryIsRanged = true;
                    }
                    report.primaryVerbProps     = effectiveVerb.verbProps;
                    report.primaryVerbDamageDef = effectiveVerb.GetDamageDef();
                }
                float rangedMul = 1;
                float meleeMul  = 1;
                if (pawn.skills != null)
                {
                    SkillRecord record;

                    record = pawn.skills.GetSkill(SkillDefOf.Shooting);
                    if (record != null)
                    {
                        rangedMul = Mathf.Lerp(0.75f, 1.75f, record.levelInt / 20f);
                    }

                    record = pawn.skills.GetSkill(SkillDefOf.Melee);
                    if (record != null)
                    {
                        meleeMul = Mathf.Lerp(0.75f, 1.75f, record.levelInt / 20f);
                    }
                }
                report.Finalize(rangedMul, meleeMul);
            }
            else
            {
                if (debug)
                {
                    collapsible.Label($"Thing {thing}({thing.thingIDNumber})[{thing.Position}]");
                }
                Verb verb = thing.TryGetAttackVerb();
                if (verb != null && !verb.IsMeleeAttack)
                {
                    report.AddVerb(verb);
                    if (debug)
                    {
                        collapsible.Label($"r.ranged rS:{Math.Round(report.rangedSharp, 2)}\trB:{Math.Round(report.rangedBlunt, 2)}\tmeta:{report.attributes}");
                        collapsible.Label($"r.ranged rSAp:{Math.Round(report.rangedSharpAp, 2)}\trBAp:{Math.Round(report.rangedBluntAp, 2)}");
                        collapsible.Label($"r.melee mS:{Math.Round(report.meleeSharp, 2)}\tmB:{Math.Round(report.meleeBlunt, 2)}");
                        collapsible.Label($"r.melee mSAp:{Math.Round(report.meleeSharpAp, 2)}\tmBAp:{Math.Round(report.meleeBluntAp, 2)}");
                    }
                    report.primaryVerbProps     = verb.verbProps;
                    report.primaryVerbDamageDef = verb.GetDamageDef();
                }
                report.primaryIsRanged = true;
                report.Finalize(1, 1);
            }
            if (debug)
            {
                collapsible.Line(4);
                collapsible.Label($"adjustedSharp:{report.adjustedSharp}\tAdjustedBlunt:{report.adjustedBlunt}");
            }
            if (report.primaryIsRanged)
            {
                report.attributes |= MetaCombatAttribute.Ranged;
            }
            else
            {
                report.attributes |= MetaCombatAttribute.Melee;
            }
            reports[thing.thingIDNumber] = report;
            return report;
        }

        public static float ThreatTo(this DamageReport damage, ArmorReport armor)
        {
            if (!damage.IsValid || !armor.IsValid)
            {
                return 0f;
            }
            if ((damage.attributes & armor.weaknessAttributes) != MetaCombatAttribute.None)
            {
                return 2f;
            }
            if (!Mod_CE.active)
            {
                return Mathf.Clamp01(2f * Maths.Max(damage.adjustedBlunt / (armor.Blunt + 1e-3f), damage.adjustedSharp / (armor.Sharp + 1e-3f), 0f));
            }
            return Mathf.Clamp01(Maths.Max(damage.adjustedBlunt / (armor.Blunt + 1e-3f), damage.adjustedSharp / (armor.Sharp + 1e-3f), 0f));
        }

        public static DamageDef GetDamageDef(this VerbProperties props)
        {
            if (props.LaunchesProjectile)
            {
                return props.defaultProjectile.projectile?.damageDef ?? null;
            }
            return props.meleeDamageDef;
        }

        public static void Invalidate(Thing thing)
        {
	        if (reports.ContainsKey(thing.thingIDNumber))
	        {
		        reports.Remove(thing.thingIDNumber);
	        }
        }

        public static void ClearCache()
        {
            reports.Clear();
        }
    }
}
