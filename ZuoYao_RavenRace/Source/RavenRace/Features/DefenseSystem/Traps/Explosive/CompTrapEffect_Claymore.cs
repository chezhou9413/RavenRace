using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;
using Verse.Sound;

namespace RavenRace
{
    public class CompTrapEffect_Claymore : CompTrapEffect
    {
        public override void OnTriggered(Pawn triggerer)
        {
            Map map = parent.Map;
            IntVec3 pos = parent.Position;
            Rot4 rot = parent.Rotation;

            // 1. 播放声音
            SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pos, map));

            // 2. 手动计算受影响的格子 (overrideCells)
            float range = 6.9f;
            float viewAngle = 90f;
            float myAngle = rot.AsAngle;

            List<IntVec3> affectedCells = new List<IntVec3>();

            // [Fixed] 显式添加中心点 (自己脚下)，确保贴脸的工兵被炸死
            affectedCells.Add(pos);

            // 获取圆形范围内的其他格子
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pos, range, true))
            {
                if (!cell.InBounds(map)) continue;
                if (cell == pos) continue; // 中心点已经手动加了，这里跳过以避免重复计算

                float angleToCell = (cell - pos).AngleFlat;
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(myAngle, angleToCell));

                if (angleDiff <= viewAngle / 2f)
                {
                    if (GenSight.LineOfSight(pos, cell, map, true))
                    {
                        affectedCells.Add(cell);
                    }
                }
            }

            // [Fixed] 为了保护背后的墙，我们尝试把它加入 ignoredThings
            // 找到背后的墙
            List<Thing> ignoredThings = new List<Thing>();
            IntVec3 wallPos = pos - rot.FacingCell;
            Thing wall = wallPos.GetEdifice(map);
            if (wall != null) ignoredThings.Add(wall);

            // 3. 执行定向爆炸
            GenExplosion.DoExplosion(
                center: pos,
                map: map,
                radius: range,
                damType: DamageDefOf.Bomb,
                instigator: parent,
                damAmount: 120, // 维持高伤害
                armorPenetration: 2.0f,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 1,
                postExplosionGasType: null,
                postExplosionGasRadiusOverride: null,
                postExplosionGasAmount: 0,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 1,
                chanceToStartFire: 0f,
                damageFalloff: true,
                direction: null,
                ignoredThings: ignoredThings, // [Fixed] 保护背后的墙
                affectedAngle: null,
                doVisualEffects: true,
                propagationSpeed: 1f,
                excludeRadius: 0f,
                doSoundEffects: true,
                postExplosionSpawnThingDefWater: null,
                screenShakeFactor: 1f,
                flammabilityChanceCurve: null,
                overrideCells: affectedCells
            );

            // 4. 销毁自身
            parent.Destroy(DestroyMode.KillFinalize);
        }
    }
}