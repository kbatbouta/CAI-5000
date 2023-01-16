namespace CombatAI.Utilities
{
#if DEBUG_REACTION
	public enum TrackedThingsRequestCategory
	{
		/// <summary>
		///     Defs that have Pawn as thingClass
		/// </summary>
		Pawns = 1,

		/// <summary>
		///     Defs that have ThingDef.IsWeapon return true
		/// </summary>
		Weapons = 4,

		/// <summary>
		///     Defs that have ThingDef.IsApparel return true
		/// </summary>
		Apparel = 8,

		/// <summary>
		///     Defs that have ThingDef.IsMedicine return true
		/// </summary>
		Medicine = 16,

		/// <summary>
		///     Defs that have ThingDef.IsMedicine return true
		/// </summary>
		Interceptors = 32
	}
#endif
}
