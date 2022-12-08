using CombatAI.Comps;
using Verse;
using System;

namespace CombatAI
{
	public class SighterTurretAnimator_Periodic : SighterTurretAnimator
	{
		private          float curRotationInt;
		private          bool  idleTurnClockwise;
		private          int   ticksUntilIdleTurn;
		
		public SighterTurretAnimator_Periodic(ThingComp_SighterTurret comp) : base(comp)
		{
		}

		/// <summary>
		///     Current turret top rotation.
		/// </summary>
		public override float CurRotation
		{
			get => curRotationInt;
			set => curRotationInt = value;
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
			if ( Math.Abs(CurRotation) > 60)
			{
				ticksUntilIdleTurn = Rand.RangeInclusive(30, 100);
			}
		}
	}
}
