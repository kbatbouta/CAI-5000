using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace CombatAI
{      
    /*
     * -----------------------------
     *
     *
     * ------ Important note -------
     * 
     * when casting update the grid at a regualar intervals for a pawn/Thing or risk exploding value issues.
     */
    [StaticConstructorOnStartup]
    public class ISignalGrid
    {        
        [StructLayout(LayoutKind.Auto)]
        private struct ISightCell
        {            
            public short expireAt;
            public short sig;            
            public ushort signalNum;                        
            public ushort signalNumPrev;            
            public float signalStrength;            
            public float signalStrengthPrev;
            
            public ISignalFields extras;
            /// <summary>
            /// Will prepare this record for the next cycle by either reseting prev fields or replacing them with the current values.
            /// </summary>            
            /// <param name="reset">Wether to reset prev</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Next(bool expired)
            {
                extras.Next(expired);
                if (!expired)
                {                                        
                    signalNumPrev       = signalNum;                    
                    signalStrengthPrev = signalStrength;                    
                }
                else
                {                    
                    signalNumPrev       = 0;            
                    signalStrengthPrev  = 0;                    
                }
            }                       
        }

        private class ISignalFields
        {
            /// <summary>
            /// The general direction of the signal.
            /// </summary>
            public Vector2 direction;
            /// <summary>
            /// The prevoius direction.
            /// </summary>
            public Vector2 directionPrev;
            /// <summary>
            /// The signal flags.
            /// </summary>
            public UInt64 flags;
            /// <summary>
            /// The prevoius signal flags.
            /// </summary>
            public UInt64 flagsPrev;

            /// <summary>
            /// Will prepare this record for the next cycle by either reseting prev fields or replacing them with the current values.
            /// </summary>            
            /// <param name="reset">Wether to reset prev</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Next(bool expired)
            {
                if (!expired)
                {
                    directionPrev = direction;
                    flagsPrev = flags;
                }
                else
                {
                    directionPrev = Vector2.zero;
                    flagsPrev = 0;
                }
            }
        }

        //
        // State fields.
        #region Fields
        
        private short sig = 13;                        
        private short cycle = 19;
        //private UInt64 currentSignalFlags;

        #endregion

        //
        // Read only fields.
        #region ReadonlyFields
        
        private readonly CellIndices cellIndices;        
        private readonly ISightCell[] signalArray;               
        private readonly Map map;
        private readonly int mapCellNum;

        #endregion

        /// <summary>
        /// The current cycle of update.
        /// </summary>
        public short CycleNum => cycle;        

        public ISignalGrid(Map map)
        {
            cellIndices = map.cellIndices;                        
            mapCellNum = cellIndices.NumGridCells;    
            signalArray = new ISightCell[mapCellNum];            
            this.map = map;            
            for (int i = 0; i < signalArray.Length; i++)
            {
                signalArray[i] = new ISightCell()
                {
                    sig = 0,
                    expireAt = 0,
                    extras = new ISignalFields()
                };
            }
        }

        public int dude()
        {
            return 0;
        }

        public void Set(IntVec3 cell, float signalStrength, Vector2 dir) => Set(cellIndices.CellToIndex(cell), signalStrength, dir);
        public void Set(int index, float signalStrength, Vector2 dir)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ISightCell record = signalArray[index];                
                if (record.sig != sig)
                {
                    IntVec3 cell = cellIndices.IndexToCell(index);                    
                    float t = record.expireAt - CycleNum;
                    if (t == 1)
                    {
                        record.signalNum++;                        
                        record.signalStrength += signalStrength;                        
                        record.extras.direction += dir;
                        // TODO remake
                        // record.extras.flags |= flags;
                    }
                    else
                    {
                        if (t == 0)
                        {
                            record.expireAt = (short)(CycleNum + 1);
                            record.Next(expired: false);                            
                        }
                        else
                        {
                            record.expireAt = (short)(CycleNum + 1);
                            record.Next(expired: true);                            
                        }
                        record.signalNum = 1;
                        record.signalStrength = signalStrength;
                        record.extras.direction = dir;
                        record.extras.flags = 0;                     
                    }
                    record.sig = sig;
                    signalArray[index] = record;
                }
            }
        }

        public void Set(IntVec3 cell, float signalStrength, Vector2 dir, UInt64 flags) => Set(cellIndices.CellToIndex(cell), signalStrength, dir, flags);
        public void Set(int index, float signalStrength, Vector2 dir, UInt64 flags)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ISightCell record = signalArray[index];
                if (record.sig != sig)
                {
                    IntVec3 cell = cellIndices.IndexToCell(index);
                    float t = record.expireAt - CycleNum;
                    if (t == 1)
                    {
                        record.signalNum++;                        
                        record.signalStrength += signalStrength;
                        record.extras.direction += dir;
                        record.extras.flags |= flags;
                    }
                    else
                    {
                        if (t == 0)
                        {
                            record.expireAt = (short)(CycleNum + 1);
                            record.Next(expired: false);
                        }
                        else
                        {
                            record.expireAt = (short)(CycleNum + 1);
                            record.Next(expired: true);
                        }
                        record.signalNum = 1;
                        record.signalStrength = signalStrength;
                        record.extras.direction = dir;
                        record.extras.flags = flags;
                    }
                    record.sig = sig;
                    signalArray[index] = record;
                }
            }
        }

        public float GetSignalNum(IntVec3 cell) => GetSignalNum(cellIndices.CellToIndex(cell));
        public float GetSignalNum(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ISightCell record = signalArray[index];
                if (record.expireAt - CycleNum == 1)                
                    return Mathf.Max(record.signalNumPrev, record.signalNum);                
                else if (record.expireAt - CycleNum == 0)
                    return record.signalNum;
            }
            return 0;
        }

        public float GetSignalStrengthAt(int index) => GetSignalStrengthAt(index, out _);
        public float GetSignalStrengthAt(IntVec3 cell) => GetSignalStrengthAt(cellIndices.CellToIndex(cell), out _);

        public float GetSignalStrengthAt(IntVec3 cell, out int signalNum) => GetSignalStrengthAt(cellIndices.CellToIndex(cell), out signalNum);
        public float GetSignalStrengthAt(int index, out int signalNum)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ISightCell record = signalArray[index];
                if (record.expireAt - CycleNum == 1)
                {
                    signalNum = Mathf.Max(record.signalNumPrev, record.signalNum);
                    return Mathf.Max(0.75f * record.signalStrengthPrev + 0.25f * signalNum, 0.75f * record.signalStrength + 0.25f * signalNum, 0f);                   
                }
                else if(record.expireAt - CycleNum == 0)
                {
                    signalNum = record.signalNum;
                    return Mathf.Max(0.75f * record.signalStrength + 0.25f * signalNum, 0f);
                }
            }
            signalNum = 0;
            return 0f;
        }        

        public UInt64 GetFlagsAt(IntVec3 cell) => GetFlagsAt(cellIndices.CellToIndex(cell));
        public UInt64 GetFlagsAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ISightCell record = signalArray[index];
                if (record.expireAt - CycleNum == 1)                      
                    return record.extras.flags | record.extras.flagsPrev;                
                else if (record.expireAt - CycleNum == 0)
                    return record.extras.flags;
            }
            return 0;
        }

        public Vector2 GetSignalDirectionAt(IntVec3 cell) => GetSignalDirectionAt(cellIndices.CellToIndex(cell));
        public Vector2 GetSignalDirectionAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ISightCell record = signalArray[index];
                if (record.expireAt - CycleNum == 1)
                {
                    if (record.signalNum > record.signalNumPrev)
                        return record.extras.direction / (record.signalNum + 0.01f);
                    else
                        return record.extras.directionPrev / (record.signalNumPrev + 0.01f);
                }
                else if (record.expireAt - CycleNum == 0)
                    return record.extras.direction / (record.signalNum + 0.01f);

            }
            return Vector2.zero;
        }      

        /// <summary>
        /// Prepare the grid for a new casting operation.
        /// </summary>
        /// <param name="center">Center of casting.</param>
        /// <param name="range">Expected range of casting.</param>
        /// <param name="casterFlags">caster's Flags</param>
        public void Next()
        {            
            if (sig++ == short.MaxValue)
                sig = 19;    
        }

        public void NextCycle()
        {
            if (sig++ == short.MaxValue)
            {
                sig = 19;
            }
            if (cycle++ == short.MaxValue)
            {
                cycle = 13;
            }            
        }

        private static StringBuilder _builder = new StringBuilder();

        public string GetDebugInfoAt(IntVec3 cell) => GetDebugInfoAt(map.cellIndices.CellToIndex(cell));
        public string GetDebugInfoAt(int index)
        {
            if (index >= 0 && index < mapCellNum)
            {
                ISightCell record = signalArray[index];
                _builder.Clear();
                _builder.AppendFormat("<color=grey>{0}</color> {1}\n", "Partially expired ", record.expireAt - CycleNum == 0);
                _builder.AppendFormat("<color=grey>{0}</color> {1}", "Expired           ", record.expireAt - CycleNum < 0);
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n" +
                    "<color=grey>cur</color>  {2}\t" +
                    "<color=grey>prev</color> {3}", "Enemies", GetSignalNum(index), record.signalNum, record.signalNumPrev);
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n " +
                    "<color=grey>cur</color>  {2}\t" +
                    "<color=grey>prev</color> {3}", "Visibility", GetSignalStrengthAt(index), Math.Round(record.signalStrength, 2), Math.Round(record.signalStrengthPrev, 2));
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n" +
                    "<color=grey>cur</color>  {2} " +
                    "<color=grey>prev</color> {3}", "Direction", GetSignalDirectionAt(index), record.extras.direction, record.extras.directionPrev);
                _builder.AppendLine();
                _builder.AppendFormat("<color=orange>{0}</color> {1}\n" +
                    "<color=grey>cur</color>\n{2}\n" +
                    "<color=grey>prev</color>\n{3}", "Flags", Convert.ToString((long)GetFlagsAt(index), 2).Replace("1", "<color=green>1</color>"), Convert.ToString((long)record.extras.flags, 2).Replace("1", "<color=green>1</color>"), Convert.ToString((long)record.extras.flagsPrev, 2).Replace("1", "<color=green>1</color>"));
                return _builder.ToString();
            }
            return "<color=red>Out of bounds</color>";
        }
    }
}

