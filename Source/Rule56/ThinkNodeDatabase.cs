using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public class ThinkNodeDatabase
    {
        private static readonly HashSet<ThinkNode> roots = new HashSet<ThinkNode>();
        private static readonly List<ThinkNode>    queue = new List<ThinkNode>();
        
        public static readonly Dictionary<ThinkNode, DutyDef>      dutyNodes  = new Dictionary<ThinkNode, DutyDef>();
        public static readonly Dictionary<ThinkNode, ThinkTreeDef> treeNodes  = new Dictionary<ThinkNode, ThinkTreeDef>();
        public static readonly Dictionary<ThinkNode, ThinkNode>    nodesNodes = new Dictionary<ThinkNode, ThinkNode>();
        
        public static void Initialize()
        {
            Log.Message("Initialize thinknodes");
            foreach (DutyDef duty in DefDatabase<DutyDef>.AllDefs)
            {
                if (duty.thinkNode != null)
                {
                    dutyNodes[duty.thinkNode] = duty;
                    roots.Add(duty.thinkNode);
                }   
            }
            foreach (ThinkTreeDef tree in DefDatabase<ThinkTreeDef>.AllDefs)
            {
                if (tree.thinkRoot != null)
                {
                    treeNodes[tree.thinkRoot] = tree;
                    roots.Add(tree.thinkRoot);
                }
            }
            foreach (DutyDef duty in DefDatabase<DutyDef>.AllDefs)
            {
                if (duty.thinkNode != null)
                {
                    BuildNodeTree(duty.thinkNode);
                }   
            }
            foreach (ThinkTreeDef tree in DefDatabase<ThinkTreeDef>.AllDefs)
            {
                if (tree.thinkRoot != null)
                {
                    BuildNodeTree(tree.thinkRoot);
                }
            }
        }

        public static void GetTrace(ThinkNode head, List<string> trace)
        {
            trace.Add(GetTraceMessage(head));
            ThinkNode node = head;
            while (node != null)
            {
                trace.Add(GetTraceMessage(node));
                if (dutyNodes.TryGetValue(node, out DutyDef duty))
                {
                    trace.Add(GetTraceMessage(duty));
                }
                if (treeNodes.TryGetValue(node, out ThinkTreeDef tree))
                {
                    trace.Add(GetTraceMessage(tree));
                }
                nodesNodes.TryGetValue(node, out node);
            }
        }

        private static void BuildNodeTree(ThinkNode root)
        {
            if (root.subNodes != null && !nodesNodes.ContainsKey(root))
            {
                queue.Clear();
                queue.Add(root);
                while (queue.Count > 0)
                {
                    ThinkNode node = queue[0];
                    queue.RemoveAt(0);
                    if (node.subNodes != null)
                    {
                        foreach (ThinkNode child in node.subNodes)
                        {
                            if (!nodesNodes.ContainsKey(child) && !roots.Contains(child))
                            {
                                nodesNodes[child] = node;
                                queue.Add(child);
                            }
                        }
                    }
                }
            }
        }

        private static string GetTraceMessage(ThinkNode node)
        {
            if (node is ThinkNode_Subtree subtree)
            {
                return $"{node.GetType()}(treeDef={subtree.treeDef})";    
            }
            return $"{node.GetType()}()";
        }
        
        private static string GetTraceMessage(DutyDef def)
        {
            return $"{def.defName}";
        }
        
        private static string GetTraceMessage(ThinkTreeDef def)
        {
            return $"{def.defName}";
        }
    }
}
