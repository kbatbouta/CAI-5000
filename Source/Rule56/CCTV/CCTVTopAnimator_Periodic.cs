using System;
using CombatAI.Comps;
using Verse;
namespace CombatAI
{
    public class CCTVTopAnimator_Periodic : CCTVTopAnimator
    {
        private bool  idleTurnClockwise;
        private float rot;
        private int   ticksUntilIdleTurn;

        public CCTVTopAnimator_Periodic(ThingComp_CCTVTop comp) : base(comp)
        {
        }

        /// <summary>
        ///     Current turret top rotation.
        /// </summary>
        public override float CurRotation
        {
            get => rot;
            set => rot = value;
        }

        public override void Tick()
        {
            if (ticksUntilIdleTurn > 0)
            {
                ticksUntilIdleTurn--;
                if (ticksUntilIdleTurn <= 0)
                {
                    idleTurnClockwise = !idleTurnClockwise;
                }
                return;
            }
            if (idleTurnClockwise)
            {
                CurRotation += 0.26f;
            }
            else
            {
                CurRotation -= 0.26f;
            }
            if (Math.Abs(CurRotation) > 60)
            {
                ticksUntilIdleTurn = Rand.RangeInclusive(30, 100);
            }
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref rot, "rot");
        }
    }
}
