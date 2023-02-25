using System.Collections.Generic;
using CombatAI.Comps;
using Verse;
namespace CombatAI
{
    public abstract class CCTVTopAnimator : IExposable
    {
        public ThingComp_CCTVTop comp;

        public CCTVTopAnimator()
        {
        }

        public CCTVTopAnimator(ThingComp_CCTVTop comp)
        {
            this.comp = comp;
        }

        public abstract float CurRotation
        {
            get;
            set;
        }
        public abstract void ExposeData();

        public abstract void Tick();

        public virtual IEnumerable<Gizmo> GetGizmos()
        {
            yield break;
        }
    }
}
