using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace CombatAI.Comps
{
    public class ThingComp_CCTVTop : ThingComp_Sighter
    {
        public  CCTVTopAnimator animator;
        private bool            cachedActive;

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
        public new CompProperties_CCTVTop Props
        {
            get => props as CompProperties_CCTVTop;
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

        public override void PostPostMake()
        {
            base.PostPostMake();
            if (animator == null)
            {
                animator = (CCTVTopAnimator)Activator.CreateInstance((props as CompProperties_CCTVTop).animator, this);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComp_Fast<WallCCTVTracker>().Register(this);
        }

        /// <summary>
        ///     Called after the turret base had finished drawing.
        /// </summary>
        public override void PostDraw()
        {
            base.PostDraw();
            CompProperties_CCTVTop props    = Props;
            float                  rot      = CurRotation;
            Vector3                position = props.graphicData.drawOffset.RotatedBy(rot);
            Matrix4x4              matrix   = default;
            matrix.SetTRS(parent.DrawPos + Altitudes.AltIncVect + position, rot.ToQuat(), new Vector3(props.graphicData.drawSize.x, 1, props.graphicData.drawSize.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, props.turretTopMat, 0);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref animator, "animator", this);
            if (animator == null)
            {
                animator = (CCTVTopAnimator)Activator.CreateInstance((props as CompProperties_CCTVTop).animator, this);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            return animator?.GetGizmos();
        }
    }
}
