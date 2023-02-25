using System;
using UnityEngine;
using Verse;
namespace CombatAI.Comps
{
    public class CompProperties_CCTVTop : CompProperties_Sighter
    {
        public Type animator;
        [Unsaved(allowLoading: false)]
        public float baseWidth;
        public float       fieldOfView;
        public GraphicData graphicData;
        [Unsaved(allowLoading: false)]
        public Material turretTopMat;
        public bool wallMounted;

        public CompProperties_CCTVTop()
        {
            compClass = typeof(ThingComp_CCTVTop);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            baseWidth = Mathf.Sin(0.5f * fieldOfView * Mathf.PI / 180f) * radius * 2;
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                turretTopMat = MaterialPool.MatFrom(graphicData.texPath);
            });
        }
    }
}
