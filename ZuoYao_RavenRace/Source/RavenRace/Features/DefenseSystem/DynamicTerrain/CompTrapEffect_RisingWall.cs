using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace RavenRace
{
    public class CompTrapEffect_RisingWall : CompTrapEffect
    {
        public override void OnTriggered(Pawn triggerer)
        {
            Map map = parent.Map;
            IntVec3 center = parent.Position;

            // 1. 播放触发特效
            SoundDefOf.MechanoidsWakeUp.PlayOneShot(new TargetInfo(center, map));
            if (map == Find.CurrentMap) Find.CameraDriver.shaker.DoShake(1.0f);

            // 2. 读取配置的大小
            int size = RavenRaceMod.Settings.risingWallSize;
            int radius = (size - 1) / 2;

            List<IntVec3> wallCells = new List<IntVec3>();

            // 3. 计算空心正方形 (四条边)
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (Mathf.Abs(x) == radius || Mathf.Abs(z) == radius)
                    {
                        wallCells.Add(center + new IntVec3(x, 0, z));
                    }
                }
            }

            ThingDef wallDef = DefenseDefOf.Raven_TrapWall;

            foreach (IntVec3 cell in wallCells)
            {
                if (!cell.InBounds(map)) continue;

                // A. 推开该位置上的 Pawn
                List<Pawn> pawnsOnCell = new List<Pawn>();
                foreach (Thing t in cell.GetThingList(map))
                {
                    if (t is Pawn p) pawnsOnCell.Add(p);
                }

                foreach (Pawn p in pawnsOnCell)
                {
                    PushPawn(p, center, map);
                }

                // B. 清理障碍物
                List<Thing> thingsToWipe = cell.GetThingList(map).FindAll(t =>
                    t.def.category == ThingCategory.Item ||
                    t.def.category == ThingCategory.Filth ||
                    t.def.category == ThingCategory.Plant);

                for (int i = thingsToWipe.Count - 1; i >= 0; i--)
                {
                    thingsToWipe[i].Destroy();
                }

                // C. 生成墙体
                if (cell.GetEdifice(map) == null)
                {
                    Building_RavenTrapWall wall = (Building_RavenTrapWall)GenSpawn.Spawn(wallDef, cell, map);
                    wall.SetFaction(parent.Faction);
                    wall.Initialize(2500); // 持续时间
                    FleckMaker.ThrowDustPuff(cell, map, 1.0f);
                }
            }

            // 4. [Fixed] 生成屋顶 (覆盖整个正方形区域)
            // 之前的 GenRadial 是圆形的，无法覆盖正方形的角落
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    IntVec3 cell = center + new IntVec3(x, 0, z);
                    if (cell.InBounds(map) && !map.roofGrid.Roofed(cell))
                    {
                        map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
                    }
                }
            }

            // 5. 销毁陷阱本体
            parent.Destroy(DestroyMode.KillFinalize);
        }

        private void PushPawn(Pawn p, IntVec3 center, Map map)
        {
            IntVec3 diff = p.Position - center;
            if (diff == IntVec3.Zero) diff = new IntVec3(1, 0, 0);

            IntVec3 dir = new IntVec3(
                Mathf.Clamp(diff.x, -1, 1),
                0,
                Mathf.Clamp(diff.z, -1, 1)
            );

            IntVec3 target = p.Position + dir;

            if (target.Walkable(map))
            {
                p.Position = target;
                p.Notify_Teleported();
            }
            else
            {
                IntVec3 randomSpot = RCellFinder.RandomWanderDestFor(p, p.Position, 2f, null, Danger.Deadly);
                if (randomSpot.IsValid)
                {
                    p.Position = randomSpot;
                    p.Notify_Teleported();
                }
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            if (RavenRaceMod.Settings.enableDefenseSystemDebug)
            {
                int size = RavenRaceMod.Settings.risingWallSize;
                float radius = (size - 1) / 2f;
                // 仅作示意，GenDraw 只有圆圈
                GenDraw.DrawRadiusRing(parent.Position, radius + 0.5f);
            }
        }
    }
}