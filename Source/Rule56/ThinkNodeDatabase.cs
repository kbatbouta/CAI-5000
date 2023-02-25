using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
namespace CombatAI
{
    public class ThinkNodeDatabase
    {
        private static readonly StringBuilder      builder = new StringBuilder();
        private static readonly HashSet<ThinkNode> roots   = new HashSet<ThinkNode>();
        private static readonly List<ThinkNode>    queue   = new List<ThinkNode>();

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

        public static void GetTrace(ThinkNode head, Pawn pawn, List<string> trace)
        {
            trace.Add(GetTraceMessage(head, pawn));
            ThinkNode node = head;
            while (node != null)
            {
                trace.Add(GetTraceMessage(node, pawn));
                if (dutyNodes.TryGetValue(node, out DutyDef duty))
                {
                    trace.Add(GetTraceMessage(duty, pawn));
                }
                if (treeNodes.TryGetValue(node, out ThinkTreeDef tree))
                {
                    trace.Add(GetTraceMessage(tree, pawn));
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

        private static string GetTraceMessage(ThinkNode node, Pawn pawn)
        {
            builder.Clear();
            int index = -1;
            if (nodesNodes.TryGetValue(node, out ThinkNode parent))
            {
                index = parent.subNodes.IndexOf(node);
            }
            else if (treeNodes.TryGetValue(node, out ThinkTreeDef treeDef))
            {
                index = treeDef.thinkRoot.subNodes.IndexOf(node);
            }
            else if (dutyNodes.TryGetValue(node, out DutyDef dutyDef))
            {
                index = dutyDef.thinkNode.subNodes.IndexOf(node);
            }
            if (node is ThinkNode_Subtree subtree)
            {
                builder.AppendFormat("(p{0}, {1})(treeDef={2})", index, node.GetType(), subtree.treeDef);
            }
            else if (node is ThinkNode_Duty dutyNode)
            {
                builder.AppendFormat("(p{0}, {1})(dutyDef={2})", index, node.GetType(), pawn.mindState.duty?.def.defName);
            }
            else
            {
                builder.AppendFormat("(p{0}, {1})", index, node.GetType());
            }
            return builder.ToString();
        }

        private static string GetTraceMessage(DutyDef def, Pawn pawn)
        {
            return $"{def.defName}";
        }

        private static string GetTraceMessage(ThinkTreeDef def, Pawn pawn)
        {
            return $"{def.defName}";
        }
    }
}
