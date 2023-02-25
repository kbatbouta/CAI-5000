using System.Collections.Generic;
using Verse;
namespace CombatAI
{
    public class PawnKindDefExtension : DefModExtension
    {
        private readonly List<MetaCombatAttribute> strongAttributes = new List<MetaCombatAttribute>();

        private readonly List<MetaCombatAttribute> weakAttributes = new List<MetaCombatAttribute>();
        [Unsaved(allowLoading: false)]
        private bool _inited;
        [Unsaved(allowLoading: false)]
        private MetaCombatAttribute _strength;
        [Unsaved(allowLoading: false)]
        private MetaCombatAttribute _weakness;

        public MetaCombatAttribute WeakCombatAttribute
        {
            get
            {
                if (!_inited)
                {
                    Init();
                }
                return _weakness;
            }
        }

        public MetaCombatAttribute StrongCombatAttribute
        {
            get
            {
                if (!_inited)
                {
                    Init();
                }
                return _strength;
            }
        }

        private void Init()
        {
            _inited   = true;
            _weakness = weakAttributes.Sum();
            _strength = strongAttributes.Sum();
        }
    }
}
