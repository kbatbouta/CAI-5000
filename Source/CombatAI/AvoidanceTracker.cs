using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatAI
{
    public class AvoidanceTracker : MapComponent
    {
        public class AvoidanceReader
        {
            public IShortTermMemoryGrid danger;
            public IShortTermMemoryGrid dangerMajor;
            public IShortTermMemoryGrid smoke;
            public IShortTermMemoryGrid pathing;
            public IShortTermMemoryGrid proximity;
            public IShortTermMemoryGrid bullet;            

            private readonly Map map;
            private readonly CellIndices indices;
            private readonly AvoidanceTracker tacker;

            public AvoidanceReader(AvoidanceTracker tracker)
            {
                this.tacker = tracker;
                this.map = tracker.map;
                this.indices = tracker.map.cellIndices;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetDanger(IntVec3 cell) => GetDanger(indices.CellToIndex(cell));
            public float GetDanger(int index) =>
                Mathf.Min(danger[index] + (AnySmoke(index) ? 4f : 0), 16f) + dangerMajor[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetPathing(IntVec3 cell) => GetPathing(indices.CellToIndex(cell));
            public float GetPathing(int index) => pathing != null ? pathing[index] + dangerMajor[index] : 0f;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float GetProximity(IntVec3 cell) => GetProximity(indices.CellToIndex(cell));
            public float GetProximity(int index) =>
                proximity != null ? Mathf.Min(proximity[index] + (AnyBullets(index) ? 2f : 0f), 16f) + dangerMajor[index] : 0f;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AnyBullets(IntVec3 cell) => AnyBullets(indices.CellToIndex(cell));
            public bool AnyBullets(int index) =>
                bullet != null ? bullet[index] > 0f : false;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AnySmoke(IntVec3 cell) => AnySmoke(indices.CellToIndex(cell));
            public bool AnySmoke(int index) =>
                smoke != null ? smoke[index] > 0 : false;
        }

        public IShortTermMemoryHandler danger;
        public IShortTermMemoryHandler dangerMajor;
        public IShortTermMemoryHandler smoke;
        public IShortTermMemoryHandler bullets;       
        public IShortTermMemoryHandler[] pathing = new IShortTermMemoryHandler[2];
        public IShortTermMemoryHandler[] proximity = new IShortTermMemoryHandler[2];        

        public AvoidanceTracker(Map map) : base(map)
        {
            danger = new IShortTermMemoryHandler(map);
            dangerMajor = new IShortTermMemoryHandler(map, 480, 40);
            bullets = new IShortTermMemoryHandler(map);
            smoke = new IShortTermMemoryHandler(map);
            pathing[0] = new IShortTermMemoryHandler(map, 80, 8);
            proximity[0] = new IShortTermMemoryHandler(map, 80, 16);
            pathing[1] = new IShortTermMemoryHandler(map, 120, 16);            
            proximity[1] = new IShortTermMemoryHandler(map, 120, 16);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            // tick bullet grid
            bullets.Tick();
            // tick smoke grid
            smoke.Tick();
            // tick danger grid
            danger.Tick();
            // tick major danger grid
            dangerMajor.Tick();
            // update path avoidence grid
            pathing[0].Tick();
            pathing[1].Tick();
            // update formation avoidence grid
            proximity[0].Tick();
            proximity[1].Tick();

            //if (Controller.settings.DebugDrawAvoidance && GenTicks.TicksGame % 15 == 0)
            //{                
            //    IntVec3 center = UI.MouseMapPosition().ToIntVec3();
            //    if (center.InBounds(map))
            //    {
            //        foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 64, true))
            //        {
            //            if (cell.InBounds(map))
            //            {
            //                var value = danger.grid[cell] + pathing[1].grid[cell] + proximity[1].grid[cell] + smoke.grid[cell] + bullets.grid[cell];
            //                if (value > 0)
            //                    map.debugDrawer.FlashCell(cell, (float)dangerMajor.grid[cell] / 20f, $"{Math.Round(danger.grid[cell], 1)} {Math.Round(dangerMajor.grid[cell], 1)} {Math.Round(pathing[1].grid[cell], 1)} {Math.Round(proximity[1].grid[cell], 1)}", 15);
            //                if (danger.grid[cell] > 0)
            //                    map.debugDrawer.FlashCell(cell, (float)danger.grid[cell] / 20f, $" ", 15);
            //                if (pathing[1].grid[cell] > 0)
            //                    map.debugDrawer.FlashCell(cell, (float)pathing[1].grid[cell] / 20f, $" ", 15);
            //                if (proximity[1].grid[cell] > 0)
            //                    map.debugDrawer.FlashCell(cell, (float)proximity[1].grid[cell] / 20f, $" ", 15);

            //            }
            //        }
            //    }
            //}
        }

        public bool TryGetReader(Pawn pawn, out AvoidanceReader reader)
        {
            reader = null;
            if (pawn.Faction == null
                || (!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid)
                || map.ParentFaction == null)                            
                return false;            
            reader = new AvoidanceReader(this);
            reader.danger = danger.grid;
            reader.dangerMajor = danger.grid;
            reader.bullet = bullets.grid;
            if (!pawn.RaceProps.IsMechanoid)
                reader.smoke = smoke.grid;
            if (!pawn.Faction.HostileTo(map.ParentFaction))
            {
                reader.proximity = !pawn.RaceProps.IsMechanoid ? proximity[0].grid : null;
                reader.pathing = pathing[0].grid;
            }
            else
            {
                reader.proximity = !pawn.RaceProps.IsMechanoid ? proximity[1].grid : null;
                reader.pathing = pathing[1].grid;
            }            
            return true;
        }

        public void Notify_Bullet(IntVec3 cell)
        {           
            if (cell.InBounds(map))
                bullets.Flood(cell, 0.35f, 3);
        }

        public void Notify_PawnSuppressed(IntVec3 cell)
        {         
            if (cell.InBounds(map))
            {
                danger.Flood(cell, 5, 4);
                //if (!PerformanceTracker.TpsCriticallyLow)
                //{
                    bullets.Set(cell, 2, 3);
                    pathing[0].Set(cell, 5, 3);
                    pathing[1].Set(cell, 5, 3);
                //}
                proximity[0].Flood(cell, 3, 5);
                proximity[1].Flood(cell, 3, 5);
                dangerMajor.Flood(cell, 0.35f, 7);
            }
        }

        public void Notify_PawnHunkered(IntVec3 cell)
        {
            if (cell.InBounds(map))
            {
                danger.Flood(cell, 10, 7);                
                //if (!PerformanceTracker.TpsCriticallyLow)
                //{
                    pathing[0].Set(cell, 10, 4);
                    pathing[1].Set(cell, 10, 4);
                    bullets.Set(cell, 10, 5);
                //}
                proximity[0].Flood(cell, 4, 4);
                proximity[1].Flood(cell, 3, 4);
                dangerMajor.Flood(cell, 0.50f, 10);
            }
        }

        public void Notify_BulletImpact(IntVec3 cell)
        {
            //if (!PerformanceTracker.TpsCriticallyLow && cell.InBounds(map))
            //{
                danger.Set(cell, 4, 3);                
                bullets.Set(cell, 2, 1);
                dangerMajor.Flood(cell, 0.15f, 15);
            //}
        }

        public void Notify_Smoke(IntVec3 cell)
        {
            //if (!PerformanceTracker.TpsCriticallyLow && cell.InBounds(map))
                smoke.Flood(cell, 0.5f, 3);
        }        

        public void Notify_PathFound(Pawn pawn, PawnPath path)
        {
            if ((!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid)
                || pawn.Faction == null
                || map.ParentFaction == null)
                return;
            IShortTermMemoryHandler manager = !pawn.Faction.HostileTo(map.ParentFaction) ? pathing[0] : pathing[1];
            for (int i = 3; i < path.nodes.Count; i += 7)                            
                manager.Set(path.nodes[i], 3, 4);
            for (int i = 1; i < path.nodes.Count; i += 3)
                manager.Set(path.nodes[i], 2, 2);            
        }

        public void Notify_CoverPositionSelected(Pawn pawn, IntVec3 cell)
        {
            if (!pawn.RaceProps.Humanlike
                || pawn.Faction == null
                || map.ParentFaction == null)
                return;
            if (cell.InBounds(map))
            {
                IShortTermMemoryHandler manager = !pawn.Faction.HostileTo(map.ParentFaction) ? proximity[0] : proximity[1];
                manager.Set(cell, 4f, 2);
                manager.Set(cell, 2f, 4);           
            }
        }

        public void Notify_Injury(Pawn pawn, IntVec3 cell)
        {
            if ((!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid)
                || pawn.Faction == null
                || map.ParentFaction == null)
                return;            
            if (cell.InBounds(map))
            {
                danger.Set(cell, 5, 7);
                pathing[0].Flood(cell, 5, 10);
                pathing[1].Flood(cell, 5, 10);
                proximity[0].Flood(cell, 3, 15);
                proximity[1].Flood(cell, 3, 15);
                dangerMajor.Flood(cell, 0.5f, 15);
            }
        }

        public void Notify_Death(Pawn pawn, IntVec3 cell)
        {
            if ((!pawn.RaceProps.Humanlike && !pawn.RaceProps.IsMechanoid)
                || pawn.Faction == null
                || map.ParentFaction == null)
                return;
            if (cell.InBounds(map))
            {
                danger.Set(cell, 8, 10);
                pathing[0].Flood(cell, 10, 15);
                pathing[1].Flood(cell, 10, 15);
                proximity[0].Flood(cell, 7, 20);
                proximity[1].Flood(cell, 7, 20);
                dangerMajor.Flood(cell, 3, 20);
            }
        }
    }
}

