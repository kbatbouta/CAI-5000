using Verse;
namespace CombatAI.Comps
{
    public class ThingComp_Statistics : ThingComp
    {

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
