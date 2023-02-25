using HarmonyLib;
using UnityEngine;
using Verse;
namespace CombatAI.Gui
{
    public static class Patcher
    {
        public static void PatchAll(Harmony harmony)
        {
            //if (patched)
            //{
            //    throw new InvalidOperationException("Attempted to patch Font Again");
            //}
            //patched = true;
            //Action<string, string, bool> patcher = (tName, pName, getterOnly) =>
            //{
            //    Type type = AccessTools.TypeByName(tName);
            //    harmony.Patch(AccessTools.PropertyGetter(type, pName), prefix: new HarmonyMethod(AccessTools.TypeByName($"{tName}_{pName}_Patch"), "Getter"));
            //    if (!getterOnly)
            //    {
            //        harmony.Patch(AccessTools.PropertySetter(type, pName), prefix: new HarmonyMethod(AccessTools.TypeByName($"{tName}_{pName}_Patch"), "Setter"));
            //    }
            //};
            //patcher("Text", "Anchor", false);
            //patcher("Text", "WordWrap", false);
            //patcher("Text", "Font", false);
            //patcher("Text", "CurFontStyle", true);
            //patcher("Text", "CurTextAreaReadOnlyStyle", true);
            //patcher("Text", "CurTextAreaStyle", true);
            //patcher("Text", "CurTextFieldStyle", true);
        }

        [HarmonyPatch(typeof(Text))]
        private static class Text_Anchor_Setter_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(Text.Anchor), MethodType.Getter)]
            public static bool Getter(ref TextAnchor __result)
            {
                if (GUIFont.UseCustomFonts)
                {
                    __result = GUIFont.Anchor;
                    return false;
                }
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(Text.Anchor), MethodType.Setter)]
            public static bool Setter(TextAnchor value)
            {
                if (GUIFont.UseCustomFonts)
                {
                    GUIFont.Anchor = value;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Text))]
        private static class Text_WordWrap_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(Text.WordWrap), MethodType.Getter)]
            public static bool Getter(ref bool __result)
            {
                if (GUIFont.UseCustomFonts)
                {
                    __result = GUIFont.WordWrap;
                    return false;
                }
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(Text.WordWrap), MethodType.Setter)]
            public static bool Setter(bool value)
            {
                if (GUIFont.UseCustomFonts)
                {
                    GUIFont.WordWrap = value;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Text))]
        private static class Text_Font_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(Text.Font), MethodType.Getter)]
            public static bool Getter(ref GameFont __result)
            {
                if (GUIFont.UseCustomFonts)
                {
                    switch (GUIFont.Font)
                    {
                        case GUIFontSize.Tiny:
                        case GUIFontSize.Smaller:
                            __result = GameFont.Tiny;
                            break;
                        case GUIFontSize.Small:
                            __result = GameFont.Small;
                            break;
                        case GUIFontSize.Medium:
                            __result = GameFont.Medium;
                            break;
                    }
                    return false;
                }
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(Text.Font), MethodType.Setter)]
            public static bool Setter(ref GameFont value)
            {
                if (GUIFont.UseCustomFonts)
                {
                    switch (value)
                    {
                        case GameFont.Tiny:
                            GUIFont.Font = GUIFontSize.Tiny;
                            break;
                        case GameFont.Small:
                            GUIFont.Font = GUIFontSize.Small;
                            break;
                        case GameFont.Medium:
                            GUIFont.Font = GUIFontSize.Medium;
                            break;
                    }
                    return false;
                }
                return true;
            }
        }


        [HarmonyPatch]
        private static class Text_CurFontStyle_Patch
        {
            [HarmonyPatch(typeof(Text), nameof(Text.CurFontStyle), MethodType.Getter)]
            public static bool Prefix(ref GUIStyle __result)
            {
                if (GUIFont.UseCustomFonts)
                {
                    __result = GUIFont.CurFontStyle;

                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private static class Text_CurTextAreaReadOnlyStyle_Patch
        {
            [HarmonyPatch(typeof(Text), nameof(Text.CurTextAreaReadOnlyStyle), MethodType.Getter)]
            public static bool Prefix(ref GUIStyle __result)
            {
                if (GUIFont.UseCustomFonts)
                {
                    __result = GUIFont.CurTextAreaReadOnlyStyle;

                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private static class Text_CurTextAreaStyle_Patch
        {
            [HarmonyPatch(typeof(Text), nameof(Text.CurTextAreaStyle), MethodType.Getter)]
            public static bool Prefix(ref GUIStyle __result)
            {
                if (GUIFont.UseCustomFonts)
                {
                    __result = GUIFont.CurTextAreaReadOnlyStyle;

                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private static class Text_CurTextFieldStyle_Patch
        {
            [HarmonyPatch(typeof(Text), nameof(Text.CurTextFieldStyle), MethodType.Getter)]
            public static bool Prefix(ref GUIStyle __result)
            {
                if (GUIFont.UseCustomFonts)
                {
                    __result = GUIFont.CurTextFieldStyle;

                    return false;
                }
                return true;
            }
        }
    }
}
