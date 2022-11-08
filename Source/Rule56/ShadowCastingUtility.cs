using System;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.BaseGen;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using Verse.Noise;

namespace CombatAI
{
    /// <summary>
    /// TL;DR Cast shadows.
    /// 
    /// The algorithem used is a modified version of   
    /// https://www.albertford.com/shadowcasting/
    /// </summary>
    public static partial class ShadowCastingUtility
    {
        private const int VISIBILITY_CARRY_MAX = 5;
        
        private static readonly Func<IntVec3, IntVec3>[] _transformationFuncs;        
        private static readonly Func<IntVec3, IntVec3>[] _transformationInverseFuncs;
        private static readonly Func<Vector3, Vector3>[] _transformationFuncsV3;
        private static readonly Func<Vector3, Vector3>[] _transformationInverseFuncsV3;

        private static readonly Vector3 InvalidOffset = new Vector3(0, -1, 0);        

        private class CastRequest
        {            
            public VisibleRow firstRow;
            public int carryLimit = 5;
            public IntVec3 source;                       
            public Map map;
            public WallGrid grid;
            public Action<IntVec3, int, int, float> setAction;
        }      

        static ShadowCastingUtility()
        {
            _transformationFuncs = new Func<IntVec3, IntVec3>[4];            
            _transformationFuncs[0] = (cell) =>
            {
                return cell;
            };
            _transformationFuncs[1] = (cell) =>
            {
                return new IntVec3(cell.z, 0, -1 * cell.x);
            };
            _transformationFuncs[2] = (cell) =>
            {
                return new IntVec3(-1 * cell.x, 0, -1 * cell.z);
            };
            _transformationFuncs[3] = (cell) =>
            {
                return new IntVec3(-1 * cell.z, 0, cell.x);
            };           
            _transformationInverseFuncs = new Func<IntVec3, IntVec3>[4];
            _transformationInverseFuncs[0] = (cell) =>
            {
                return cell;
            };
            _transformationInverseFuncs[1] = (cell) =>
            {
                return new IntVec3(-1 * cell.z, 0, cell.x);
            };
            _transformationInverseFuncs[2] = (cell) =>
            {
                return new IntVec3(-1 * cell.x, 0, -1 * cell.z);
            };
            _transformationInverseFuncs[3] = (cell) =>
            {
                return new IntVec3(cell.z, 0, -1 * cell.x);
            };
            _transformationFuncsV3 = new Func<Vector3, Vector3>[4];
            _transformationFuncsV3[0] = (cell) =>
            {
                return cell;
            };
            _transformationFuncsV3[1] = (cell) =>
            {
                return new Vector3(cell.z, 0, -1 * cell.x);
            };
            _transformationFuncsV3[2] = (cell) =>
            {
                return new Vector3(-1 * cell.x, 0, -1 * cell.z);
            };
            _transformationFuncsV3[3] = (cell) =>
            {
                return new Vector3(-1 * cell.z, 0, cell.x);
            };
            _transformationInverseFuncsV3 = new Func<Vector3, Vector3>[4];
            _transformationInverseFuncsV3[0] = (cell) =>
            {
                return cell;
            };
            _transformationInverseFuncsV3[1] = (cell) =>
            {
                return new Vector3(-1 * cell.z, 0, cell.x);
            };
            _transformationInverseFuncsV3[2] = (cell) =>
            {
                return new Vector3(-1 * cell.x, 0, -1 * cell.z);
            };
            _transformationInverseFuncsV3[3] = (cell) =>
            {
                return new Vector3(cell.z, 0, -1 * cell.x);
            };
        }

        private struct VisibleRow
        {
            public float startSlope;
            public float endSlope;

            public int visibilityCarry;            
            public int depth;
            public int quartor;
            public int maxDepth;
            public float blockChance;

            public IEnumerable<Vector3> Tiles()
            {
                int min = (int)Mathf.Floor(this.startSlope * this.depth + 0.5f);
                int max = (int)Mathf.Ceil(this.endSlope * this.depth - 0.5f);
                
                for (int i = min; i <= max; i++)
                {                    
                    yield return new Vector3(depth, 0, i);
                }
            }

            public void DebugFlash(Map map, IntVec3 root)
            {
                float startSlope = this.startSlope;
                float endSlope = this.endSlope;
                int min = (int)Mathf.Floor(startSlope * this.depth + 0.5f);
                int max = (int)Mathf.Ceil(endSlope * this.depth - 0.5f);
                IntVec3 left = root + _transformationFuncs[quartor](new IntVec3(this.depth, 0, min));
                IntVec3 right = root + _transformationFuncs[quartor](new IntVec3(this.depth, 0, max));
                int depth = this.depth;
                map.GetComponent<MapComponent_CombatAI>().EnqueueMainThreadAction(() =>
                {
                    if (left.InBounds(map))
                    {
                        map.debugDrawer.FlashCell(left, 0.99f, $"{Math.Round(startSlope, 3)}");
                    }
                    if (right.InBounds(map))
                    {
                        map.debugDrawer.FlashCell(right, 0.01f, $"{Math.Round(endSlope, 3)}");
                    }
                });
            }

            public IntVec3 Transform(IntVec3 oldOffset)
            {
                return _transformationFuncs[quartor](oldOffset);
            }

            public VisibleRow Next()
            {
                VisibleRow row = new VisibleRow();
                row.endSlope = this.endSlope;
                row.startSlope = this.startSlope;
                row.depth = this.depth + 1;
                row.maxDepth = maxDepth;
                row.quartor = this.quartor;
                row.visibilityCarry = visibilityCarry;
                row.blockChance = blockChance;
                return row;
            }

            public static VisibleRow First
            {
                get
                {
                    VisibleRow row = new VisibleRow();
                    row.startSlope = -1;
                    row.endSlope = 1;
                    row.depth = 1;
                    row.visibilityCarry = 1;
                    return row;
                }
            }
        }       

        private static bool IsValid(Vector3 point) => point.y >= 0;
        private static float GetSlope(Vector3 point) => (2f * point.z - 1.0f) / (2f * point.x);

        private static int GetQurator(IntVec3 cell)
        {
            int x = cell.x;
            int z = cell.z;
            if (x > 0 && Math.Abs(z) < x)
            {
                return 0;
            }
            else if (x < 0 && Math.Abs(z) < Math.Abs(x))
            {
                return 2;
            }
            else if (z > 0 && Math.Abs(x) <= z)
            {
                return 3;
            }
            return 1;
        }        

        private static void TryWeightedScan(CastRequest request)
        {
            int cellsScanned = 0;
            WallGrid grid = request.grid;
            if (grid == null)
            {
                Log.Error($"Wall grid not found for {request.map} with cast center {request.source}");
                return;
            }
            List<VisibleRow> rowQueue = new List<VisibleRow>();            
            rowQueue.Add(request.firstRow);
            float coverMinDist = Mathf.Max(request.firstRow.maxDepth / 3f, 10f);
            while (rowQueue.Count > 0)
            {                
                VisibleRow nextRow;                
                VisibleRow row = rowQueue.Pop();
               
                if (row.depth > row.maxDepth || row.visibilityCarry <= 1e-5f)
                {
                    continue;
                }
                //row.DebugFlash(request.map, request.source);
                var lastCell = InvalidOffset;                
                var lastIsWall = false;
                var lastIsCover = false;
                var lastFill = 0f;
                var lastFillNum = 0;

                foreach (Vector3 offset in row.Tiles())
                {
                    FillCategory fill = FillCategory.None;
                    IntVec3 cell = request.source + row.Transform(offset.ToIntVec3());                
                    bool isWall = !cell.InBounds(request.map) || (fill = grid.GetFillCategory(cell)) == FillCategory.Full;
                    bool isCover = !isWall && fill == FillCategory.Partial;
                    
                    if (isWall || (offset.z >= row.depth * row.startSlope && offset.z <= row.depth * row.endSlope))
                    {
                        request.setAction(cell, row.visibilityCarry, row.depth, row.blockChance);
                    }            
                    if (isCover)
                    { 
                        if (row.visibilityCarry >= request.carryLimit)                        
                        {
                            isCover = false;
                            isWall = true;
                        }
                        else
                        {
                            lastFill += grid[cell];
                            lastFillNum++;
                        }
                    }
                    if (IsValid(lastCell)) // check so it's a valid offsets
                    {
                        if (lastIsWall == isWall)
                        {
                            if (isCover != lastIsCover && lastIsWall == isWall) // first handle cover 
                            {
                                nextRow = row.Next();
                                nextRow.endSlope = GetSlope(offset);
                                if (lastIsCover && row.depth > 6)
                                {
                                    nextRow.blockChance = Mathf.Max((1 - row.blockChance) * lastFill / lastFillNum * Mathf.Lerp(0, 1f, (row.depth - 6f) / coverMinDist), row.blockChance);
                                    nextRow.visibilityCarry += 1;
                                }
                                rowQueue.Add(nextRow);
                                row.startSlope = GetSlope(offset);
                            }
                        }
                        else if (!isWall && lastIsWall)
                        {
                            row.startSlope = GetSlope(offset);
                        }
                        else if (isWall && !lastIsWall)
                        {
                            nextRow = row.Next();
                            nextRow.endSlope = GetSlope(offset);
                            if (lastIsCover && row.depth > 6)
                            {
                                nextRow.blockChance = Mathf.Max((1 - row.blockChance) * lastFill / lastFillNum * Mathf.Lerp(0, 1f, (row.depth - 6f) / coverMinDist), row.blockChance);
                                nextRow.visibilityCarry += 1;
                            }
                            rowQueue.Add(nextRow);
                        }                        
                    }
                    cellsScanned++;
                    lastCell = offset;
                    lastIsWall = isWall;
                    lastIsCover = isCover;
                }                
                if (lastCell.y >= 0 && !lastIsWall)
                {
                    nextRow = row.Next();
                    if (lastIsCover && row.depth > 6)
                    {                        
                        nextRow.blockChance = Mathf.Max((1 - row.blockChance) * lastFill / lastFillNum * Mathf.Lerp(0, 1f, (row.depth - 6f) / coverMinDist), row.blockChance);
                        nextRow.visibilityCarry += 1;
                    }
                    rowQueue.Add(nextRow);
                }
            }
        }      

        private static void TryVisibilityScan(CastRequest request)
        {
            int cellsScanned = 0;
            WallGrid grid = request.grid;
            if (grid == null)
            {
                Log.Error($"Wall grid not found for {request.map} with cast center {request.source}");
                return;
            }
            List<VisibleRow> rowQueue = new List<VisibleRow>();            
            rowQueue.Add(request.firstRow);
            while (rowQueue.Count > 0)
            {
                VisibleRow nextRow;
                VisibleRow row = rowQueue.Pop();
                
                if (row.depth > row.maxDepth)
                {
                    continue;
                }
                var lastCell = InvalidOffset;
                var lastIsWall = false;
                
                foreach (Vector3 offset in row.Tiles())
                {
                    var cell = request.source + row.Transform(offset.ToIntVec3());
                    var isWall = !cell.InBounds(request.map) || !grid.CanBeSeenOver(cell);                    

                    if (isWall || (offset.z >= row.depth * row.startSlope && offset.z <= row.depth * row.endSlope))
                    {
                        request.setAction(cell, 1, row.depth, row.blockChance);
                    }
                    if (IsValid(lastCell)) // check so it's a valid offsets
                    {                        
                        if (!isWall && lastIsWall)
                        {
                            row.startSlope = GetSlope(offset);
                        }
                        else if (isWall && !lastIsWall)
                        {
                            nextRow = row.Next();
                            nextRow.endSlope = GetSlope(offset);                               
                            rowQueue.Add(nextRow);
                        }                        
                    }
                    cellsScanned++;                    
                    lastCell = offset;
                    lastIsWall = isWall;
                }
                if (lastCell.y >= 0 && !lastIsWall)
                {                                    
                    rowQueue.Add(row.Next());
                }
            }
        }

        private static void TryCastVisibility(float startSlope, float endSlope, int quartor, int maxDepth, int carryLimit, IntVec3 source, Map map, Action<IntVec3, int, int, float> setAction) => TryCast(TryVisibilityScan, startSlope, endSlope, quartor, maxDepth, carryLimit, source, map, setAction);
        private static void TryCastWeighted(float startSlope, float endSlope, int quartor, int maxDepth, int carryLimit, IntVec3 source, Map map, Action<IntVec3, int, int, float> setAction) => TryCast(TryWeightedScan, startSlope, endSlope, quartor, maxDepth, carryLimit, source, map, setAction);

        private static void TryCast(Action<CastRequest> castAction, float startSlope, float endSlope, int quartor, int maxDepth, int carryLimit, IntVec3 source, Map map, Action<IntVec3, int, int, float> setAction)
        {            
            if (endSlope > 1.0f || startSlope < -1 || startSlope > endSlope)
            {
                throw new InvalidOperationException($"CE: Scan quartor {quartor} endSlope and start slop must be between (-1, 1) but got start:{startSlope}\tend:{endSlope}");
            }            
            WallGrid grid = map.GetComponent<WallGrid>();
            if(grid == null)
            {
                Log.Error($"Wall grid not found for {map} with cast center {source}");
                return;
            }  
            IntVec3 coverLoc = source + _transformationFuncs[quartor](new IntVec3(1, 0, 0));
            if (coverLoc.InBounds(map) && grid.GetFillCategory(coverLoc) == FillCategory.Full)
            {
                IntVec3 rightUpper = source + _transformationFuncs[quartor](new IntVec3(1, 0, -1));
                IntVec3 rightLower = source + _transformationFuncs[quartor](new IntVec3(0, 0, -1));
                bool rightBlocked = !rightUpper.InBounds(map) || grid.GetFillCategory(rightUpper) == FillCategory.Full || !rightLower.InBounds(map) || grid.GetFillCategory(rightLower) == FillCategory.Full;
                IntVec3 leftUpper = source + _transformationFuncs[quartor](new IntVec3(1, 0, 1));
                IntVec3 leftLower = source + _transformationFuncs[quartor](new IntVec3(0, 0, 1));
                bool leftBlocked = !leftUpper.InBounds(map) || grid.GetFillCategory(leftUpper) == FillCategory.Full || !leftLower.InBounds(map) || grid.GetFillCategory(leftLower) == FillCategory.Full;
                if (rightBlocked != leftBlocked || (!leftBlocked && !rightBlocked))
                {                    
                    if (!leftBlocked)
                    {
                        CastRequest requestLeft = new CastRequest();
                        VisibleRow arcLeft = VisibleRow.First;
                        arcLeft.startSlope = !Finder.Settings.LeanCE_Enabled ? startSlope : (-1f / Mathf.Max(maxDepth, 64));
                        arcLeft.endSlope = endSlope;                        
                        arcLeft.visibilityCarry = 1;
                        arcLeft.maxDepth = maxDepth - 1;
                        arcLeft.quartor = quartor;
                        requestLeft.firstRow = arcLeft;
                        requestLeft.grid = grid;
                        requestLeft.carryLimit = carryLimit;
                        requestLeft.map = map;
                        requestLeft.source = leftLower;
                        requestLeft.setAction = setAction;
                        castAction(requestLeft);
                    }
                    if (!rightBlocked)
                    {
                        CastRequest requestRight = new CastRequest();
                        VisibleRow arcRight = VisibleRow.First;                        
                        arcRight.startSlope = startSlope;
                        arcRight.endSlope = !Finder.Settings.LeanCE_Enabled ? endSlope : (1f / Mathf.Max(maxDepth, 64));                        
                        arcRight.visibilityCarry = 1;
                        arcRight.maxDepth = maxDepth - 1;
                        arcRight.quartor = quartor;
                        requestRight.firstRow = arcRight;
                        requestRight.grid = grid;
                        requestRight.carryLimit = carryLimit;
                        requestRight.map = map;
                        requestRight.source = rightLower;
                        requestRight.setAction = setAction;
                        castAction(requestRight);
                    }
                }
            }
            else
            {
                CastRequest request = new CastRequest();
                VisibleRow arc = VisibleRow.First;
                arc.startSlope = startSlope;
                arc.endSlope = endSlope;
                arc.visibilityCarry = 1;
                arc.maxDepth = maxDepth;
                arc.quartor = quartor;
                request.firstRow = arc;
                request.grid = grid;
                request.carryLimit = carryLimit;
                request.map = map;
                request.source = source;
                request.setAction = setAction;
                castAction(request);
            }
        }                         
    }
}

