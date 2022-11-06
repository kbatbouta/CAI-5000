using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CombatAI
{
    public class CellFlooder
    {       
        public struct Node : IComparable<Node>
        {
            public IntVec3 cell;
            public IntVec3 parent;            
            public float dist;
            public float distAbs;

            public int CompareTo(Node other)
            {
                return dist.CompareTo(other.dist) * -1;   
            }
        }        

        private static readonly IntVec3[] offsets = new IntVec3[4]
        {
            new IntVec3(-1, 0, 0),
            new IntVec3(0, 0, 1),
            new IntVec3(1, 0, 0),
            new IntVec3(0, 0, -1),            
        };

        public Map map;

        private int sig;
        private WallGrid walls;
        private readonly FastHeap<Node> floodQueue = new FastHeap<Node>();        
        private readonly int[] sigArray;
        //
        // private readonly List<Node> floodedCells = new List<Node>();

        public CellFlooder(Map map)
        {
            this.sig = 13;
            this.map = map;
            this.walls = map.GetComponent<WallGrid>();
            this.sigArray = new int[map.cellIndices.NumGridCells]; 
        }

        public void Flood(IntVec3 center, Action<IntVec3, IntVec3, float> action, Func<IntVec3, float> costFunction = null, Func<IntVec3, bool> validator = null, int maxDist = 25) => Flood(center, (node) => action(node.cell, node.parent, node.dist), costFunction, validator, maxDist);

        public void Flood(IntVec3 center, Action<Node> action, Func<IntVec3, float> costFunction = null, Func<IntVec3, bool> validator = null, int maxDist = 25)
        {
            sig++;
            Func<IntVec3, bool> blocked = GetBlockedTestFunc(validator);
            this.walls = map.GetComponent<WallGrid>();
            Node node = GetIntialFloodedCell(center);
            Node nextNode;
            int cellIndex;            
            IntVec3 nextCell;
            IntVec3 offset;            
            //
            // floodedCells.Clear();
            // floodedCells.Add(node);
            floodQueue.Clear();
            floodQueue.Enqueue(GetIntialFloodedCell(center));
            while (floodQueue.Count > 0)
            {
                node = floodQueue.Dequeue();
                //
                // TODO optimize this some more
                action(node);

                // map.debugDrawer.FlashCell(node.cell, node.dist / 25f, $"{map.cellIndices.CellToIndex(node.cell)} {map.cellIndices.CellToIndex(node.parent)}", duration: 15);
                //
                // check for the distance
                if (node.distAbs >= maxDist)
                {
                    continue;
                }
                for (int i = 0; i < 4; i++)
                {
                    offset = offsets[i];
                    nextCell = node.cell + offset;
                    if (nextCell.InBounds(map))
                    {
                        if (sigArray[cellIndex = map.cellIndices.CellToIndex(nextCell)] != sig)
                        {
                            sigArray[cellIndex] = sig;
                            if (!blocked(nextCell))
                            {
                                nextNode = new Node();
                                nextNode.cell = nextCell;
                                // TODO improve this.
                                // this is not perfectly accurate but it does result in consistant result.
                                if (Mathf.Abs(nextCell.x - node.parent.x) == 1 && Mathf.Abs(nextCell.z - node.parent.z) == 1)
                                {
                                    nextNode.parent = node.parent;
                                    nextNode.dist = node.dist + 0.4123f;
                                    nextNode.distAbs = node.distAbs + 0.4123f;
                                }
                                else
                                {
                                    nextNode.parent = node.cell;                                   
                                    nextNode.dist = node.dist + 1;
                                    nextNode.distAbs = node.distAbs + 1;
                                }
                                if (costFunction != null)
                                {
                                    nextNode.dist += costFunction(nextCell);
                                }
                                floodQueue.Enqueue(nextNode);
                                //
                                //floodedCells.Add(nextNode);
                            }
                        }
                    }                    
                }                
            }            
        }

        private Func<IntVec3, bool> GetBlockedTestFunc(Func<IntVec3, bool> validator)
        {
            if (validator == null)
            {
                return (cell) => walls.GetFillCategory(cell) == FillCategory.Full;
            }
            else
            {
                return (cell) => walls.GetFillCategory(cell) == FillCategory.Full || !validator(cell);
            }
        }        

        private Node GetIntialFloodedCell(IntVec3 center)
        {
            Node cell = new Node();
            cell.cell = center;
            cell.parent = center;
            cell.dist = 0;            
            return cell;
        }       
    }
}

