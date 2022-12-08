using UnityEngine;
using Verse;
using System;

namespace CombatAI.Comps
{
	public class CompProperties_CCTVTop : CompProperties_Sighter
	{
		public GraphicData graphicData;
		public float       fieldOfView;
		public Type        animator;
		public bool        wallMounted;
		[Unsaved(allowLoading:false)]
		public float baseWidth;
		[Unsaved(allowLoading: false)]
		public Material turretTopMat;
		
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
