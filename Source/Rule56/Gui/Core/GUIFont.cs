using System;
using UnityEngine;
using Verse;
namespace CombatAI.Gui
{
    public enum GUIFontSize
    {
        Tiny = 0, Smaller = 1, Small = 2, Medium = 3
    }

    public static class GUIFont
    {
        public static bool UseCustomFonts;

        private static GUIFontSize fontInt;

        private static readonly GUIStyle[] fontStyles;

        private static readonly GUIStyle[] textFieldStyles;

        private static readonly GUIStyle[] textAreaStyles;

        private static readonly GUIStyle[] textAreaReadOnlyStyles;

        private static readonly Font[] fonts;

        private static readonly float[] lineHeights;

        private static readonly float[] spaceBetweenLines;

        private static readonly GUIContent tmpTextGUIContent;

        private static bool wordWrapInt;

        private static TextAnchor anchorInt;

        private static readonly GUIStyle[] tempfontStyles;

        private static readonly GUIStyle[] temptextFieldStyles;

        private static readonly GUIStyle[] temptextAreaStyles;

        private static readonly GUIStyle[] temptextAreaReadOnlyStyles;

        static GUIFont()
        {
            UseCustomFonts = true;

            fontInt = GUIFontSize.Tiny;
            fonts   = new Font[4];

            fontStyles             = new GUIStyle[4];
            textFieldStyles        = new GUIStyle[4];
            textAreaStyles         = new GUIStyle[4];
            textAreaReadOnlyStyles = new GUIStyle[4];

            tempfontStyles             = new GUIStyle[4];
            temptextFieldStyles        = new GUIStyle[4];
            temptextAreaStyles         = new GUIStyle[4];
            temptextAreaReadOnlyStyles = new GUIStyle[4];

            lineHeights       = new float[4];
            spaceBetweenLines = new float[4];
            tmpTextGUIContent = new GUIContent();
            anchorInt         = TextAnchor.UpperLeft;
            wordWrapInt       = true;

            fonts[0] = (Font)Resources.Load("Fonts/Calibri_tiny");
            fonts[1] = (Font)Resources.Load("Fonts/Arial_small");
            fonts[2] = (Font)Resources.Load("Fonts/Arial_small");
            fonts[3] = (Font)Resources.Load("Fonts/Arial_medium");

            fontStyles[0]               = new GUIStyle(GUI.skin.label);
            fontStyles[0].font          = fonts[0];
            fontStyles[1]               = new GUIStyle(GUI.skin.label);
            fontStyles[1].font          = fonts[1];
            fontStyles[1].contentOffset = new Vector2(0f, -1f);
            fontStyles[1].fontSize      = 13;
            fontStyles[2]               = new GUIStyle(GUI.skin.label);
            fontStyles[2].font          = fonts[2];
            fontStyles[2].contentOffset = new Vector2(0f, -1f);
            fontStyles[3]               = new GUIStyle(GUI.skin.label);
            fontStyles[3].font          = fonts[3];

            for (int i = 0; i < textFieldStyles.Length; i++)
            {
                textFieldStyles[i]           = new GUIStyle(GUI.skin.textField);
                textFieldStyles[i].alignment = TextAnchor.MiddleLeft;
            }

            textFieldStyles[0].font = fonts[0];
            textFieldStyles[1].font = fonts[1];
            textFieldStyles[2].font = fonts[2];
            textFieldStyles[3].font = fonts[3];

            for (int j = 0; j < textAreaStyles.Length; j++)
            {
                textAreaStyles[j]           = new GUIStyle(textFieldStyles[j]);
                textAreaStyles[j].alignment = TextAnchor.UpperLeft;
                textAreaStyles[j].wordWrap  = true;
            }
            for (int k = 0; k < textAreaReadOnlyStyles.Length; k++)
            {
                textAreaReadOnlyStyles[k] = new GUIStyle(textAreaStyles[k]);
                GUIStyle obj;
                obj                      = textAreaReadOnlyStyles[k];
                obj.normal.background    = null;
                obj.active.background    = null;
                obj.onHover.background   = null;
                obj.hover.background     = null;
                obj.onFocused.background = null;
                obj.focused.background   = null;
            }
            RestorStyles();

            UseCustomFonts = true;

            int index = 0;
            foreach (GUIFontSize value in new[]
                     {
                         GUIFontSize.Tiny, GUIFontSize.Smaller, GUIFontSize.Small, GUIFontSize.Medium
                     })
            {
                Font                     = value;
                lineHeights[index]       = CalcHeight("W", 999f);
                spaceBetweenLines[index] = CalcHeight("W\nW", 999f) - Text.CalcHeight("W", 999f) * 2f;
                index++;
            }
            fontInt = GUIFontSize.Tiny;

            UseCustomFonts = false;
        }


        public static TextAnchor Anchor
        {
            get
            {
                if (UseCustomFonts)
                {
                    return anchorInt;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
            set
            {
                if (UseCustomFonts)
                {
                    anchorInt = value;

                    Text.anchorInt = value;
                    return;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
        }


        public static bool WordWrap
        {
            get
            {
                if (UseCustomFonts)
                {
                    return wordWrapInt;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
            set
            {
                if (UseCustomFonts)
                {
                    wordWrapInt = value;

                    Text.wordWrapInt = value;
                    return;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
        }

        public static GUIFontSize Font
        {
            get
            {
                if (UseCustomFonts)
                {
                    return fontInt;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
            set
            {
                if (UseCustomFonts)
                {
                    fontInt = value;
                    return;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
        }

        public static GUIStyle CurFontStyle
        {
            get
            {
                if (UseCustomFonts)
                {
                    GUIStyle style = tempfontStyles[0];
                    switch (fontInt)
                    {
                        case GUIFontSize.Tiny:
                            style = tempfontStyles[0];
                            break;
                        case GUIFontSize.Smaller:
                            style = tempfontStyles[1];
                            break;
                        case GUIFontSize.Small:
                            style = tempfontStyles[2];
                            break;
                        case GUIFontSize.Medium:
                            style = tempfontStyles[3];
                            break;
                    }
                    style.alignment = anchorInt;
                    style.wordWrap  = wordWrapInt;
                    return style;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
        }

        public static GUIStyle CurTextAreaReadOnlyStyle
        {
            get
            {
                if (UseCustomFonts)
                {
                    GUIStyle style = temptextAreaReadOnlyStyles[0];
                    switch (fontInt)
                    {
                        case GUIFontSize.Tiny:
                            style = temptextAreaReadOnlyStyles[0];
                            break;
                        case GUIFontSize.Smaller:
                            style = temptextAreaReadOnlyStyles[1];
                            break;
                        case GUIFontSize.Small:
                            style = temptextAreaReadOnlyStyles[2];
                            break;
                        case GUIFontSize.Medium:
                            style = temptextAreaReadOnlyStyles[3];
                            break;
                    }
                    return style;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
        }

        public static GUIStyle CurTextAreaStyle
        {
            get
            {
                if (UseCustomFonts)
                {
                    GUIStyle style = temptextAreaStyles[0];
                    switch (fontInt)
                    {
                        case GUIFontSize.Tiny:
                            style = temptextAreaStyles[0];
                            break;
                        case GUIFontSize.Smaller:
                            style = temptextAreaStyles[1];
                            break;
                        case GUIFontSize.Small:
                            style = temptextAreaStyles[2];
                            break;
                        case GUIFontSize.Medium:
                            style = temptextAreaStyles[3];
                            break;
                    }
                    return style;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
        }

        public static GUIStyle CurTextFieldStyle
        {
            get
            {
                if (UseCustomFonts)
                {
                    GUIStyle style = temptextFieldStyles[0];
                    switch (fontInt)
                    {
                        case GUIFontSize.Tiny:
                            style = temptextFieldStyles[0];
                            break;
                        case GUIFontSize.Smaller:
                            style = temptextFieldStyles[1];
                            break;
                        case GUIFontSize.Small:
                            style = temptextFieldStyles[2];
                            break;
                        case GUIFontSize.Medium:
                            style = temptextFieldStyles[3];
                            break;
                    }
                    return style;
                }
                throw new Exception("Used GUIFont.CurFontStyle outside a safe scope");
            }
        }

        public static void RestorStyles()
        {
            for (int i = 0; i < 4; i++)
            {
                tempfontStyles[i]             = new GUIStyle(fontStyles[i]);
                temptextFieldStyles[i]        = new GUIStyle(textFieldStyles[i]);
                temptextAreaStyles[i]         = new GUIStyle(textAreaStyles[i]);
                temptextAreaReadOnlyStyles[i] = new GUIStyle(textAreaReadOnlyStyles[i]);
            }
        }

        public static float CalcHeight(string text, float width)
        {
            tmpTextGUIContent.text = text.StripTags();
            return CurFontStyle.CalcHeight(tmpTextGUIContent, width);
        }

        public static Vector2 CalcSize(string text)
        {
            tmpTextGUIContent.text = text.StripTags();
            return CurFontStyle.CalcSize(tmpTextGUIContent);
        }
    }
}
