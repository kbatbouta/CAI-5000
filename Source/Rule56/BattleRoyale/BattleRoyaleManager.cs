using System;
using System.Collections.Generic;
using System.Linq;
using CombatAI.Arena;
using NAudio.SoundFont;
using Verse;

namespace CombatAI
{
	public class BattleRoyaleManager : GameComponent
	{
		private bool _active;	
				
		public BattleRoyaleManager(Game game)
		{
			BattleRoyale.manager = this;
			_active = false;
			BattleRoyale.generator = new BattleRoyaleGenerator();			
		}

		public IEnumerable<MapBattleRoyale> Battles
		{
			get => Find.Maps.Select(m => m.GetComp_Fast<MapBattleRoyale>());
		}

		public bool Active
		{
			get => _active;
			set
			{				
				_active = value;
			}
		}

		public override void GameComponentTick()
		{
			base.GameComponentTick();			
			if(!Active)
			{
				return;
			}			
			if (GenTicks.TicksGame % 240 == 0)
			{
				TimeSpeed curSpeed = Find.TickManager.CurTimeSpeed;
				if (curSpeed == TimeSpeed.Fast || curSpeed == TimeSpeed.Superfast)
				{
					if (!TickManager.UltraSpeedBoost)
					{
						TickManager.UltraSpeedBoost = true;
					}
					Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
				}
				foreach (MapBattleRoyale br in Battles)
				{
					if (!br.Active)
					{
						StartMapBattle(br);
					}
				}
				if (GenTicks.TicksGame % 480 == 0)
				{
					BattleRoyale.generator.GeneratorUpdate();
				}
			}
		}

		public void Start()
		{			
			_active = true;
			foreach (MapBattleRoyale br in Battles)
			{
				br.Stop();
				StartMapBattle(br);
			}
		}

		public void Stop()
		{			
			_active = false;
			foreach (MapBattleRoyale br in Battles)
			{
				br.Stop();
			}
		}	

		private void StartMapBattle(MapBattleRoyale mapBattle)
		{
			BattleRoyaleParms parms = BattleRoyale.generator.Next();
			if (parms.IsValid)
			{							
				mapBattle.StartBattleRoyale(parms);				
			}
		}
	}
}

