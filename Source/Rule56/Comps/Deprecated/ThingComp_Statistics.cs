using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using Verse.AI;
using System.Threading;
using CombatAI.Statistics;
using System.Text;

namespace CombatAI.Comps
{
	public class ThingComp_Statistics : ThingComp
	{
		public ThingComp_Statistics()
		{
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
		}

		public override void CompTickRare()
		{
		}

		public void Notify_PawnTookDamage()
		{
		}

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
		}
	}
}