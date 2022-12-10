using CombatAI.Comps;
namespace CombatAI
{
	public abstract class CCTVTopAnimator
	{
		public ThingComp_CCTVTop comp;

		public CCTVTopAnimator(ThingComp_CCTVTop comp)
		{
			this.comp = comp;
		}

		public abstract float CurRotation
		{
			get;
			set;
		}

		public abstract void Tick();
	}
}
