using System.Reflection;
namespace CombatAI
{
    [LoadIf("Murmur.OOCMoveSpeedBoost")]
    public class Mod_MoveSpeed
    {
        public static bool active;

        [LoadNamed("MURSpeedMod.S:mult")]
        public static FieldInfo S;

        [LoadNamed("MURSpeedMod.SpeedModSettings:boostToggle")]
        public static FieldInfo boostToggle;

        public static float Mult
        {
            //get => active && (bool)boostToggle.GetValue(null) ? (float) S.GetValue(null) : 1f;
            get => 1f;
        }

        [RunIf(loaded: true)]
        private static void OnActive()
        {
            active = boostToggle != null && S != null;
        }
    }
}
