using System;
using System.Collections.Generic;
using Verse;
namespace CombatAI
{
    public class AIAgentData : IExposable
    {
        /* Fields
         * ---------------------------------------------------------   
         */
        
        #region Fields
        
        private AIEnvThings enemies;
        private AIEnvThings allies;

        #endregion
        
        /* 
         * ---------------------------------------------------------   
         */
        
        public AIAgentData()
        {
            enemies = new AIEnvThings();
            allies  = new AIEnvThings();
        }
        
        /* Timestamps
         * ---------------------------------------------------------   
         */
        
        #region Timestamps

        public int LastTookDamage
        {
            get;
            set;
        }
        public int lastRetreated
        {
            get;
            set;
        }
        public int LastInterrupted
        {
            get;
            set;
        }
        public int LastScanned
        {
            get;
            set;
        }

        #endregion
        
        /* Environment
         * ---------------------------------------------------------   
         */
        
        #region Spotting
        
        public AIEnvThings AllEnemies
        {
            get => enemies.AsReadonly;
        }
        public int NumEnemies
        {
            get;
            set;
        }
        public IEnumerator<AIEnvAgentInfo> EnemiesVisible()
        {
            return enemies.GetEnumerator(AIEnvAgentState.visible);
        }
        public IEnumerator<AIEnvAgentInfo> EnemiesNearBy()
        {
            return enemies.GetEnumerator(AIEnvAgentState.visible);
        }
        public IEnumerator<AIEnvAgentInfo> MeleeEnemiesNearBy()
        {
            return enemies.GetEnumerator(AIEnvAgentState.melee & AIEnvAgentState.melee);
        }
        public IEnumerator<AIEnvAgentInfo> EnemiesWhere(AIEnvAgentState customState)
        {
            return enemies.GetEnumerator(customState);
        }
        public void ReSetEnemies(HashSet<AIEnvAgentInfo> items)
        {
            enemies.ClearAndAddRange(items);
            NumEnemies = enemies.Count;
        }
        public void ReSetEnemies()
        {
            enemies.Clear();
            NumEnemies = 0;
        }
        
        public AIEnvThings AllAllies
        {
            get => allies.AsReadonly;
        }
        public int NumAllies
        {
            get;
            set;
        }
        public IEnumerator<AIEnvAgentInfo> AlliesNearBy()
        {
            return allies.GetEnumerator(AIEnvAgentState.nearby);
        }
        public IEnumerator<AIEnvAgentInfo> AlliesWhere(AIEnvAgentState customState)
        {
            return allies.GetEnumerator(customState);
        }
        public void ReSetAllies(HashSet<AIEnvAgentInfo> items)
        {
            allies.ClearAndAddRange(items);
            NumAllies = allies.Count;
        }
        public void ReSetAllies()
        {
            allies.Clear();
            NumAllies = 0;
        }
        
        #endregion
        
        /*
         * ---------------------------------------------------------   
         */

        public void ExposeData()
        {
//            int v = 0;
//            Scribe_Deep.Look(ref enemies, $"EnvEnemies.{v}");
//            enemies    ??= new AIEnvThings();
//            NumEnemies =   enemies.Count;
//            Scribe_Deep.Look(ref allies, $"EnvAllies.{v}");
//            allies     ??= new AIEnvThings();
//            NumAllies  =   allies.Count;
        }
    }
}
