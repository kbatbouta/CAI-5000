using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace CombatAI.Gui
{
    public class Selector_DefSelection : ISelector_GenericSelection<Def>
    {
        public Selector_DefSelection(IEnumerable<Def> defs, Action<Def> selectionAction, bool integrated = false,
            Action                                    closeAction = null) : base(defs, selectionAction, integrated, closeAction)
        {
        }

        protected override void DoSingleItem(Rect rect, Def item)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.DefLabelWithIcon(rect, item);
        }

        protected override bool ItemMatchSearchString(Def item)
        {
            return item.label?.ToLower()?.Contains(searchString.ToLower()) ?? true;
        }
    }
}
