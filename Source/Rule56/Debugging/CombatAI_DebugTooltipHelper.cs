using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
namespace CombatAI
{
    /*
     * Taken from my original commit to CE where I introduced this debugging tool.	 
     * https://github.com/CombatExtended-Continued/CombatExtended/tree/31446370a92855de32c709bcd8ff09039db85452		 
     */
    public class CombatAI_DebugTooltipHelper : GameComponent
    {

        private static readonly List<Pair<Func<Map, IntVec3, string>, KeyCode>> mapCallbacks   = new List<Pair<Func<Map, IntVec3, string>, KeyCode>>();
        private static readonly List<Pair<Func<World, int, string>, KeyCode>>   worldCallbacks = new List<Pair<Func<World, int, string>, KeyCode>>();

        private static readonly Rect          MouseRect = new Rect(0, 0, 50, 50);
        private readonly        StringBuilder builder   = new StringBuilder();

        static CombatAI_DebugTooltipHelper()
        {
            IEnumerable<MethodInfo> functions = typeof(CombatAI_DebugTooltipHelper).Assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods(AccessTools.all))
                .Where(m => m.HasAttribute<CombatAI_DebugTooltip>() && m.IsStatic);
            foreach (MethodBase m in functions)
            {
                CombatAI_DebugTooltip attribute = m.TryGetAttribute<CombatAI_DebugTooltip>();
                if (attribute.tooltipType == CombatAI_DebugTooltipType.World)
                {
                    ParameterInfo[] param = m.GetParameters();
                    if (param[0].ParameterType != typeof(World) || param[1].ParameterType != typeof(int))
                    {
                        Log.Error($"ISMA: Error processing debug tooltip {m.GetType().Name}:{m.Name} {m.FullDescription()} need to have (World, int) as parameters, skipped");
                        continue;
                    }
                    worldCallbacks.Add(new Pair<Func<World, int, string>, KeyCode>((world, tile) => (string)m.Invoke(null, new object[]
                    {
                        world, tile
                    }), attribute.altKey));
                }
                else if (attribute.tooltipType == CombatAI_DebugTooltipType.Map)
                {
                    ParameterInfo[] param = m.GetParameters();
                    if (param[0].ParameterType != typeof(Map) || param[1].ParameterType != typeof(IntVec3))
                    {
                        Log.Error($"ISMA: Error processing debug tooltip {m.GetType().Name}:{m.Name} {m.FullDescription()} need to have (Map, IntVec3) as parameters, skipped");
                        continue;
                    }
                    mapCallbacks.Add(new Pair<Func<Map, IntVec3, string>, KeyCode>((map, cell) => (string)m.Invoke(null, new object[]
                    {
                        map, cell
                    }), attribute.altKey));
                }
            }
        }

        public CombatAI_DebugTooltipHelper(Game game)
        {
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            if (!Finder.Settings.Debug || !Input.anyKey || Find.CurrentMap == null || Current.ProgramState != ProgramState.Playing)
            {
                return;
            }
            Rect mouseRect;
            mouseRect        = MouseRect;
            mouseRect.center = Event.current.mousePosition;
            Camera worldCamera = Find.WorldCamera;

            if (!worldCamera.gameObject.activeInHierarchy)
            {
                IntVec3 mouseCell = UI.MouseCell();
                if (mouseCell.InBounds(Find.CurrentMap))
                {
                    TryMapTooltips(mouseRect, mouseCell);
                }
            }
            else
            {
                int tile = GenWorld.MouseTile();
                if (tile != -1)
                {
                    TryWorldTooltips(mouseRect, tile);
                }
            }
        }

        private void TryMapTooltips(Rect mouseRect, IntVec3 mouseCell)
        {
            bool bracketShown = false;
            for (int i = 0; i < mapCallbacks.Count; i++)
            {
                Pair<Func<Map, IntVec3, string>, KeyCode> callback = mapCallbacks[i];
                if (Input.GetKey(callback.Second == KeyCode.None ? KeyCode.LeftShift : callback.Second))
                {
                    string message = callback.First(Find.CurrentMap, mouseCell);
                    if (!message.NullOrEmpty())
                    {
                        DoTipSignal(mouseRect, message, i);
                    }
                    if (!bracketShown)
                    {
                        GenUI.RenderMouseoverBracket();
                        bracketShown = true;
                    }
                }
            }
        }

        private void TryWorldTooltips(Rect mouseRect, int tile)
        {
            for (int i = 0; i < worldCallbacks.Count; i++)
            {
                Pair<Func<World, int, string>, KeyCode> callback = worldCallbacks[i];
                if (Input.GetKey(callback.Second == KeyCode.None ? KeyCode.LeftShift : callback.Second))
                {
                    string message = callback.First(Find.World, tile);
                    if (!message.NullOrEmpty())
                    {
                        DoTipSignal(mouseRect, message, i);
                    }
                }
            }
        }

        private TipSignal DoTipSignal(Rect rect, string message, int id)
        {
            builder.Clear();
            builder.Append("<color=orange>DEBUG_CAI:</color>. ");
            builder.AppendLine(message);
            TipSignal tip = new TipSignal();
            tip.text     = builder.ToString();
            tip.uniqueId = id + 1037;
            tip.priority = (TooltipPriority)3;
            TooltipHandler.TipRegion(rect, tip);
            return tip;
        }
    }
}
