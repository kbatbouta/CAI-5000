using UnityEngine;
namespace CombatAI.Gui
{
    public class HyperText
    {
        public HyperTextDef        def;
        public Listing_Collapsible collapsible;

        public HyperText(HyperTextDef def, bool allowScrolling = true)
        {
            this.def                                  = def;
            this.collapsible                          = new Listing_Collapsible(scrollViewOnOverflow: allowScrolling);
            this.collapsible.CollapsibleBGColor       = new Color(0, 0, 0, 0);
            this.collapsible.CollapsibleBGBorderColor = new Color(0, 0, 0, 0);
        }
        
        public void Draw(Rect rect)
        {
            collapsible.Expanded = true;
            collapsible.Begin(rect, "", false, false, hightlightIfMouseOver: false);
            def.DrawParts(collapsible);
            collapsible.End(ref rect);
        }
    }
}
