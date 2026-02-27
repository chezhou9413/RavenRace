using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    public class CompProperties_AbilityTenguGale : CompProperties_AbilityEffect
    {
        public float knockbackDistance = 8f;
        public ThingDef flyerDef;
        public CompProperties_AbilityTenguGale() => this.compClass = typeof(CompAbilityEffect_TenguGale);
    }

    public class CompAbilityEffect_TenguGale : CompAbilityEffect
    {
        public new CompProperties_AbilityTenguGale Props => (CompProperties_AbilityTenguGale)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;

            // 1. 基础有效性检查
            if (victim == null || caster == null) return;
            if (victim.Dead || victim.Downed) return;

            // [修复核心] 检查目标是否已经在飞行中 (防止 "already in another container" 错误)
            // 如果目标已经在飞行器里，说明上一次击退还没结束，或者产生了逻辑冲突，直接终止。
            if (victim.ParentHolder is PawnFlyer) return;

            // 2. 检查体型限制 (太大的吹不动)
            if (victim.BodySize > 3.0f)
            {
                Messages.Message("RavenRace_Msg_TargetTooLarge".Translate(victim.LabelShort), victim, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // 3. 计算击退目标点
            Vector3 pushDir = (victim.DrawPos - caster.DrawPos).normalized;
            Vector3 destPos = victim.DrawPos + pushDir * Props.knockbackDistance;
            IntVec3 destCell = destPos.ToIntVec3();

            // 4. [修复核心] 严格限制在地图边界内 (防止 "null map" 错误)
            // 留出 3 格缓冲距离，防止吹到地图边缘卡死
            destCell = ClampToMap(destCell, caster.Map, 3);

            // 寻找最近的可站立点
            destCell = FindNearestStandable(destCell, caster.Map);

            // 5. 生成飞行器
            if (Props.flyerDef != null)
            {
                // 再次确认地图不为空且人还在图上
                if (victim.Map == null || !victim.Spawned) return;

                PawnFlyer flyer = PawnFlyer.MakeFlyer(
                    Props.flyerDef,
                    victim,
                    destCell,
                    null, // 特效 (由 XML 的 Effecter 处理)
                    null  // 音效
                );

                if (flyer != null)
                {
                    GenSpawn.Spawn(flyer, victim.Position, victim.Map);
                }
            }
        }

        private IntVec3 ClampToMap(IntVec3 cell, Map map, int margin)
        {
            if (map == null) return cell;
            int x = Mathf.Clamp(cell.x, margin, map.Size.x - margin);
            int z = Mathf.Clamp(cell.z, margin, map.Size.z - margin);
            return new IntVec3(x, cell.y, z);
        }

        private IntVec3 FindNearestStandable(IntVec3 cell, Map map)
        {
            if (map == null) return cell;
            // 如果落点本身可站立，直接返回
            if (cell.Standable(map)) return cell;

            // 否则在周围寻找
            IntVec3 validCell = CellFinder.StandableCellNear(cell, map, 5f);

            // 如果找不到（比如被吹进了深山），使用正确的 RCellFinder 方法
            if (!validCell.IsValid)
            {
                // 方案1：使用 TryFindRandomSpotJustOutsideColony 方法（需要 pawn）
                IntVec3 fallbackCell;
                if (RCellFinder.TryFindRandomSpotJustOutsideColony(parent.pawn, out fallbackCell))
                {
                    return fallbackCell;
                }

                // 方案2：如果上面的方法也失败，使用 CellFinder 的备用方法
                if (CellFinder.TryFindRandomCellNear(cell, map, 10,
                    (IntVec3 c) => c.Standable(map) && !c.Fogged(map), out fallbackCell, 20))
                {
                    return fallbackCell;
                }

                // 最后的备用方案：返回原始位置
                return parent.pawn.Position;
            }
            return validCell;
        }
    }
}