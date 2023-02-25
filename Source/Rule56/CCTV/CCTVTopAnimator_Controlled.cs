using System.Collections.Generic;
using CombatAI.Comps;
using CombatAI.R;
using RimWorld;
using Verse;
namespace CombatAI
{
    public class CCTVTopAnimator_Controlled : CCTVTopAnimator_Periodic
    {
        private bool paused;

        public CCTVTopAnimator_Controlled(ThingComp_CCTVTop comp) : base(comp)
        {
        }

        public override void Tick()
        {
            if (!paused)
            {
                base.Tick();
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            Command_Toggle toggle = new Command_Toggle();
            toggle.toggleAction = () =>
            {
                paused = !paused;
            };
            toggle.isActive = () =>
            {
                return paused;
            };
            toggle.icon         = TexCommand.PauseCaravan;
            toggle.defaultLabel = Keyed.CombatAI_Animator_Controller;
            toggle.defaultDesc  = Keyed.CombatAI_Animator_Controller_Description;
            yield return toggle;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref paused, "paused");
        }
    }
}
