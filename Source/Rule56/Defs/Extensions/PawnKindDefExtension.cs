using System;
using Verse;
using System.Collections.Generic;

namespace CombatAI
{
	public class PawnKindDefExtension : DefModExtension
	{
		[Unsaved(allowLoading: false)]
		private MetaCombatAttribute _weakness;
		[Unsaved(allowLoading: false)]
		private MetaCombatAttribute _strength;
		[Unsaved(allowLoading: false)]
		private bool _inited;

		private List<MetaCombatAttribute> weakAttributes   = new List<MetaCombatAttribute>();
		private List<MetaCombatAttribute> strongAttributes = new List<MetaCombatAttribute>();

		public MetaCombatAttribute WeakCombatAttribute
		{
			get
			{
				if (!_inited)
				{
					Init();
				}
				return _weakness;
			}
		}

		public MetaCombatAttribute StrongCombatAttribute
		{
			get
			{
				if (!_inited)
				{
					Init();
				}
				return _strength;
			}
		}

		private void Init()
		{
			_inited   = true;
			_weakness = weakAttributes.Sum();
			_strength = strongAttributes.Sum();
		}
	}
}
