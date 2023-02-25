namespace CombatAI
{
    public enum InterceptorFlags
    {
        interceptOutgoingProjectiles   = 1,
        interceptAirProjectiles        = 2,
        interceptNonHostileProjectiles = 4,
        interceptGroundProjectiles     = 8
    }
}
