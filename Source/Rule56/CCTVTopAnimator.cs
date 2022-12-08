using CombatAI.Comps;
using Verse;
namespace CombatAI
{
	public abstract class CCTVTopAnimator
	{
		public ThingComp_CCTVTop comp;

		public abstract float CurRotation
		{
			get;
			set;
		}

		public CCTVTopAnimator(ThingComp_CCTVTop comp)
		{
			this.comp = comp;
		}

		public abstract void Tick();
	}
}
