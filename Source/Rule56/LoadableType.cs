namespace CombatAI
{
    public enum LoadableType
    {
        Unspecified = 0,
        Field       = 1,
        Method      = 2,
        Getter      = 4,
        Setter      = 8,
        Constructor = 16,
        Type        = 32
    }
}
