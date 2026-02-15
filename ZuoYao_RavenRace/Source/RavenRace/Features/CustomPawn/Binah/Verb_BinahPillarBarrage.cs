using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class Verb_BinahPillarBarrage : Verb_CastAbility
    {
        public void DrawWarmupEffect(Stance_Warmup warmup)
        {
            if (CasterPawn == null || !CasterPawn.Spawned) return;

            float totalTicks = this.verbProps.warmupTime.SecondsToTicks();
            if (totalTicks <= 0) totalTicks = 60f;

            float progress = 1f - ((float)warmup.ticksLeft / totalTicks);
            int visiblePillars = Mathf.FloorToInt(progress * 8f) + 1;
            visiblePillars = Mathf.Clamp(visiblePillars, 0, 8);

            Vector3 center = CasterPawn.DrawPos;
            float radius = 3.5f;

            for (int i = 0; i < visiblePillars; i++)
            {
                // 角度：从0度(北)开始顺时针
                float angle = i * 45f;

                Vector3 offset = Vector3Utility.FromAngleFlat(angle) * radius;
                Vector3 pos = center + offset;

                ThingDef projDef = GetPillarDef(i % 4);

                if (projDef != null && projDef.graphicData != null)
                {
                    Graphic graphic = projDef.graphicData.Graphic;
                    Material mat = graphic.MatSingle;

                    if (mat != null)
                    {
                        Vector3 drawPos = pos;
                        drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                        Vector2 size = projDef.graphicData.drawSize;
                        Vector3 scale = new Vector3(size.x, 2f, size.y);

                        // [Fix] 显式构建旋转四元数，确保绕 Y 轴
                        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);

                        // 构建矩阵
                        Matrix4x4 matrix = Matrix4x4.TRS(drawPos, rotation, scale);

                        // 绘制
                        Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
                    }
                }
            }
        }

        protected override bool TryCastShot()
        {
            FireAllPillars();
            return true;
        }

        private void FireAllPillars()
        {
            if (caster == null || caster.Map == null) return;
            Vector3 origin = caster.DrawPos;
            Map map = caster.Map;

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                int typeIndex = i % 4;
                ThingDef projDef = GetPillarDef(typeIndex);

                Vector3 direction = Vector3Utility.FromAngleFlat(angle);
                Vector3 targetPos = origin + direction * 200f;
                IntVec3 targetCell = targetPos.ToIntVec3();
                Vector3 spawnPos = origin + direction * 3.5f;
                IntVec3 spawnCell = spawnPos.ToIntVec3();

                if (!spawnCell.InBounds(map)) spawnCell = caster.Position;

                if (projDef != null)
                {
                    Projectile projectile = (Projectile)GenSpawn.Spawn(projDef, spawnCell, map, WipeMode.Vanish);

                    // 确保发射的投射物也对齐
                    // Projectile_Explosive 默认会朝向目标旋转，所以只要目标点对，它就会放射状飞出去。
                    projectile.Launch(caster, spawnPos, new LocalTargetInfo(targetCell), new LocalTargetInfo(targetCell), ProjectileHitFlags.All, false, null);
                }
            }
        }

        private ThingDef GetPillarDef(int index)
        {
            switch (index)
            {
                case 0: return BinahDefOf.Raven_Projectile_Binah_Pillar_I;
                case 1: return BinahDefOf.Raven_Projectile_Binah_Pillar_II;
                case 2: return BinahDefOf.Raven_Projectile_Binah_Pillar_III;
                case 3: return BinahDefOf.Raven_Projectile_Binah_Pillar_IV;
                default: return null;
            }
        }
    }
}