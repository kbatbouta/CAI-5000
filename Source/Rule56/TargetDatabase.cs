using Verse;
namespace CombatAI
{
    public class TargetDatabase
    {
        public bool isPlayerAllience;
        public Map  map;

        public TargetDatabase(Map map, bool isPlayerAllience)
        {
            this.map              = map;
            this.isPlayerAllience = isPlayerAllience;
        }
    }
}
