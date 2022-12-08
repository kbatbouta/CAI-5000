using System;
using System.Reflection;
using UnityEngine;
using Verse;
namespace CombatAI.Comps
{
	public class ThingComp_SighterTurret : ThingComp_Sighter
	{
		private bool                  cachedActive;
		public  SighterTurretAnimator animator;

		/// <summary>
		///     Current turret top rotation.
		/// </summary>
		public float CurRotation
		{
			get => animator.CurRotation - parent.Rotation.AsAngle;
		}

		public float BaseWidth
		{
			get => Props.baseWidth;
		}

		/// <summary>
		///     Cur rot as a vector.
		/// </summary>
		public Vector3 LookDirection
		{
			get => Vector3Utility.FromAngleFlat(CurRotation + 90);
		}

		/// <summary>
		///     Source CompProperties_SighterTurret.
		/// </summary>
		public new CompProperties_SighterTurret Props
		{
			get => props as CompProperties_SighterTurret;
		}

		/// <summary>
		///     Called every tick.
		/// </summary>
		public override void CompTick()
		{
			base.CompTick();
			if (parent.IsHashIntervalTick(240))
			{
				cachedActive = Active;
			}
			if (cachedActive)
			{
				animator.Tick();
			}
		}

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			animator = (SighterTurretAnimator) Activator.CreateInstance((props as CompProperties_SighterTurret).animator, new object[]{this});
		}

		/// <summary>
		///     Called after the turret base had finished drawing.
		/// </summary>
		public override void PostDraw()
		{
			base.PostDraw();
			CompProperties_SighterTurret props    = Props;
			float                        rot      = CurRotation;
			Vector3                      position = props.graphicData.drawOffset.RotatedBy(rot);
			Matrix4x4                    matrix   = default;
			matrix.SetTRS(parent.DrawPos + Altitudes.AltIncVect + position, rot.ToQuat(), new Vector3(props.graphicData.drawSize.x, 1, props.graphicData.drawSize.y));
			Graphics.DrawMesh(MeshPool.plane10, matrix, props.turretTopMat, 0);
		}
	}
}
