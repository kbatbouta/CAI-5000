using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace CombatAI.Gui
{
    public class Selector_TypeSelection : ISelector_GenericSelection<Type>
    {
        private static readonly Dictionary<Type, string> cache = new Dictionary<Type, string>();
        private readonly        int                      count;
        private readonly        Type[]                   types;
        private                 Rect                     viewRect = Rect.zero;

        public Selector_TypeSelection(Type t, Action<Type> selectionAction, bool integrated = false,
            Action                         closeAction = null) : base(t.AllSubclassesNonAbstract(), selectionAction, integrated, closeAction)
        {
            types = t.AllSubclassesNonAbstract().ToArray();
            count = types.Length;
        }

        public override float RowHeight
        {
            get => 24f;
        }

        public override void DoContent(Rect inRect)
        {
            FillTypeContent(inRect);
        }

        protected void FillTypeContent(Rect inRect)
        {
            try
            {
                GUIUtility.ScrollView(inRect, ref scrollPosition, types,
                                      type => !searchString.NullOrEmpty() ? ItemMatchSearchString(type) ? -1f : RowHeight : RowHeight,
                                      (rect, type) =>
                                      {
                                          DoSingleItem(rect, type);
                                          if (Widgets.ButtonInvisible(rect))
                                          {
                                              selectionAction.Invoke(type);
                                              if (!integrated)
                                              {
                                                  Close();
                                              }
                                          }
                                      });
            }
            catch (Exception er)
            {
                Log.Error(er.ToString());
            }
        }

        protected override void DoSingleItem(Rect rect, Type item)
        {
            string name;
            if (!cache.TryGetValue(item, out name))
            {
                name = cache[item] = item.Name.Translate();
            }
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.Label(rect, name);
        }

        protected override bool ItemMatchSearchString(Type item)
        {
            return true;
        }
    }
}
