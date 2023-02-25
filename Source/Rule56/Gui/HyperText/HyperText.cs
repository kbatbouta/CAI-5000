using UnityEngine;
namespace CombatAI.Gui
{
    public class HyperText
    {
        public Listing_Collapsible collapsible;
        public HyperTextDef        def;

        public HyperText(HyperTextDef def, bool allowScrolling = true)
        {
            this.def                             = def;
            collapsible                          = new Listing_Collapsible(scrollViewOnOverflow: allowScrolling);
            collapsible.CollapsibleBGColor       = new Color(0, 0, 0, 0);
            collapsible.CollapsibleBGBorderColor = new Color(0, 0, 0, 0);
        }

        public void Draw(Rect rect)
        {
            collapsible.Expanded = true;
            collapsible.Begin(rect, "", false, false, false);
            def.DrawParts(collapsible);
            collapsible.End(ref rect);
        }
    }
}
