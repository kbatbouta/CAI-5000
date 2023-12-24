using System;
using System.Runtime.CompilerServices;
using Verse;
namespace CombatAI
{
	[LoadIf("SmashPhil.VehicleFramework")]
	public class Mod_Vehicles
	{
		public static bool active;
		
		[LoadNamed("Vehicles.VehiclePawn")]
		public static Type Vehicle;

		[RunIf(loaded: true)]
		private static void OnActive()
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsVehicle(Pawn pawn)
		{
			return active && (Vehicle?.IsInstanceOfType(pawn) ?? false);
		}
	}
}
