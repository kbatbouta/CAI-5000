using Verse;
namespace CombatAI.Comps
{
    public class CompProperties_Sighter : CompProperties
    {
        public bool mannable;
        public bool powered;
        public int  radius;
        public int? radiusNight;
        public CompProperties_Sighter()
        {
            compClass = typeof(ThingComp_Sighter);
        }
    }
}
