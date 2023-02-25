using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using GUITextState = System.Tuple<string, CombatAI.Gui.GUIFontSize, System.Tuple<float, float>, System.Tuple<int, int, int, int>, System.Tuple<UnityEngine.FontStyle, UnityEngine.FontStyle, UnityEngine.FontStyle, UnityEngine.FontStyle>>;

namespace CombatAI.Gui
{
    public static partial class GUIUtility
    {
        private const int MAX_CACHE_SIZE = 2000;

        private static readonly Dictionary<GUITextState, float> textHeightCache = new Dictionary<GUITextState, float>(512);
        private static readonly Dictionary<GUITextState, float> textWidthCache  = new Dictionary<GUITextState, float>(512);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Cleanup()
        {
            if (textHeightCache.Count > MAX_CACHE_SIZE)
            {
                textHeightCache.Clear();
            }
        }

        public static string Fit(this string text, Rect rect)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            Cleanup();
            float width = CalcTextWidth(text);
            if (rect.width >= width)
            {
                return text;
            }
            return text.Substring(0, Mathf.FloorToInt(Mathf.Clamp(text.Length * rect.width / width - 3, 1, text.Length))) + "...";
        }

        public static float GetTextHeight(this string text, Rect rect)
        {
            return text != null ? CalcTextHeight(text, rect.width) : 0;
        }

        public static float GetTextHeight(this string text, float width)
        {
            return text != null ? CalcTextHeight(text, width) : 0;
        }

        public static float GetTextHeight(this TaggedString text, float width)
        {
            return text != null ? CalcTextHeight(text, width) : 0;
        }

        public static float CalcTextHeight(string text, float width)
        {
            Cleanup();
            GUITextState key = GetGUIState(text, width);
            if (textHeightCache.TryGetValue(key, out float height))
            {
                return height;
            }
            return textHeightCache[key] = Text.CalcHeight(text, width);
        }

        public static float CalcTextWidth(string text)
        {
            Cleanup();
            GUITextState key = GetGUIState(text, -1);
            if (textWidthCache.TryGetValue(key, out float width))
            {
                return width;
            }
            return textWidthCache[key] = Text.CalcSize(text).x;
        }

        private static GUITextState GetGUIState(string text, float width)
        {
            return new GUITextState(
                text,
                GUIFont.Font,
                new System.Tuple<float, float>(
                    width,
                    Prefs.UIScale),
                new System.Tuple<int, int, int, int>(
                    GUIFont.CurFontStyle.fontSize,
                    Text.CurTextAreaReadOnlyStyle.fontSize,
                    Text.CurTextAreaStyle.fontSize,
                    Text.CurTextFieldStyle.fontSize),
                new System.Tuple<FontStyle, FontStyle, FontStyle, FontStyle>(
                    GUIFont.CurFontStyle.fontStyle,
                    Text.CurTextAreaReadOnlyStyle.fontStyle,
                    Text.CurTextAreaStyle.fontStyle,
                    Text.CurTextFieldStyle.fontStyle));
        }
    }
}
