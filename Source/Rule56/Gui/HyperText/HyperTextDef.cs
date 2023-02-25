using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Verse;
namespace CombatAI.Gui
{
    [StaticConstructorOnStartup]
    public class HyperTextDef : Def
    {
        [Unsaved(allowLoading: false)]
        private readonly List<Action<Listing_Collapsible>> actions = new List<Action<Listing_Collapsible>>();

        public void DrawParts(Listing_Collapsible collapsible)
        {
            foreach (Action<Listing_Collapsible> part in actions)
            {
                part(collapsible);
            }
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            try
            {
                foreach (XmlNode node in xmlRoot.ChildNodes)
                {
                    if (node.Name == "content")
                    {
                        ParseXmlContent(node);
                    }
                    if (node.Name == "defName")
                    {
                        defName = node.InnerText;
                    }
                }
            }
            catch (Exception er)
            {
                Log.Error(er.ToString());
            }
        }

        private void ParseXmlContent(XmlNode xmlRoot)
        {
            foreach (XmlNode node in xmlRoot.ChildNodes)
            {
                if (node is XmlElement element)
                {
                    if (element.Name == "p")
                    {
                        ParseTextXmlNode(element);
                    }
                    else if (element.Name == "img")
                    {
                        ParseMediaNode(element);
                    }
                    else if (element.Name == "gap")
                    {
                        ParseGapNode(element);
                    }
                }
            }
        }

        private void ParseTextXmlNode(XmlElement element)
        {
            XmlAttribute fontSize = element.Attributes["fontSize"];
            if (fontSize == null || !Enum.TryParse(fontSize.Value, true, out GUIFontSize size))
            {
                size = GUIFontSize.Small;
            }
            XmlAttribute textAnchor = element.Attributes["textAnchor"];
            if (textAnchor == null || !Enum.TryParse(textAnchor.Value, true, out TextAnchor anchor))
            {
                anchor = TextAnchor.UpperLeft;
            }
            string text = element.InnerText.Replace('[', '<').Replace(']', '>');

            void Action(Listing_Collapsible collapsible)
            {
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    GUIFont.Anchor = anchor;
                    GUIFont.Font   = size;
                    collapsible.Lambda(text.GetTextHeight(collapsible.Rect.width + 20) + 5, rect =>
                    {
                        GUIFont.Anchor = anchor;
                        GUIFont.Font   = size;
                        Widgets.Label(rect, text);
                    });
                });
            }

            actions.Add(Action);
        }

        private void ParseGapNode(XmlElement element)
        {
            XmlAttribute gapHeight = element.Attributes["height"];
            if (gapHeight == null || !int.TryParse(gapHeight.Value, out int height))
            {
                height = 1;
            }

            void Action(Listing_Collapsible collapsible)
            {
                collapsible.Gap(height);
            }

            actions.Add(Action);
        }

        private void ParseMediaNode(XmlElement element)
        {
            string       path      = element.Attributes["path"].Value;
            string       heightStr = null;
            XmlAttribute imgHeight = element.Attributes["height"];
            if (imgHeight != null)
            {
                heightStr = imgHeight.Value;
            }
            int index = actions.Count;
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(path);
                int       width   = texture.width;
                if (heightStr == null || !int.TryParse(heightStr, out int height))
                {
                    height = texture.height;
                }

                void Action(Listing_Collapsible collapsible)
                {
                    collapsible.Lambda(height, rect =>
                    {
                        Widgets.DrawTextureFitted(rect, texture, 1.0f);
                    });
                }

                actions[index] = Action;
            });
            actions.Add(null);
        }
    }
}
