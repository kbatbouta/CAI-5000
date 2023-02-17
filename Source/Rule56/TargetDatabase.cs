using Verse;
namespace CombatAI
{
    public class TargetDatabase
    {
        public Map  map;
        public bool isPlayerAllience;

        public TargetDatabase(Map map, bool isPlayerAllience)
        {
            this.map              = map;
            this.isPlayerAllience = isPlayerAllience;
        }
    }
}
