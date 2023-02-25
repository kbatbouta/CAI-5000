using System.Collections.Generic;
using Verse;
namespace CombatAI.Abilities
{
    public class Pawn_AbilityCaster : IExposable
    {
        public Pawn pawn;

        public Pawn_AbilityCaster()
        {
        }

        public Pawn_AbilityCaster(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public void ExposeData()
        {
        }

        public void TickRare(HashSet<Thing> visibleEnemies)
        {
        }
    }
}
