using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace CombatAI
{
	[LoadIf("CETeam.CombatExtended")]
	public class Mod_CE
	{
		public static bool active;

		public static JobDef ReloadWeapon;
		public static JobDef HunkerDown;

		[LoadNamed("CombatExtended.Verb_ShootCE:_isAiming")]
		public static FieldInfo _isAiming;
		[LoadNamed("CombatExtended.Verb_ShootCE")]
		public static Type Verb_ShootCE;

		public static bool IsAimingCE(Verb verb)
		{			
			return _isAiming != null && Verb_ShootCE.IsInstanceOfType(verb) && (bool) _isAiming.GetValue(verb);
		}		
	}
}

