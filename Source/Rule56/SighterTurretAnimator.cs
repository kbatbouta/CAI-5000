using CombatAI.Comps;
using Verse;
namespace CombatAI
{
	public abstract class SighterTurretAnimator
	{
		public ThingComp_SighterTurret comp;

		public abstract float CurRotation
		{
			get;
			set;
		}

		public SighterTurretAnimator(ThingComp_SighterTurret comp)
		{
			this.comp = comp;
		}

		public abstract void Tick();
	}
}
