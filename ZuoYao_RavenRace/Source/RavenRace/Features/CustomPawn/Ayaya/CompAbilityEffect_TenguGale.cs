using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Ayaya
{
    public class CompProperties_AbilityTenguGale : CompProperties_AbilityEffect
    {
        /// <summary>以施法者为中心的吹飞半径（格）</summary>
        public float blastRadius = 12f;
        /// <summary>被吹飞后的击退距离（格）</summary>
        public float knockbackDistance = 14f;
        /// <summary>体型上限，超过此值的生物无法被吹飞</summary>
        public float maxBodySize = 3.0f;
        /// <summary>击退使用的 PawnFlyer ThingDef</summary>
        public ThingDef flyerDef;

        public CompProperties_AbilityTenguGale()
        {
            this.compClass = typeof(CompAbilityEffect_TenguGale);
        }
    }

    /// <summary>
    /// 天狗颪效果组件
    /// 以施法者（文文）为中心，大范围吹飞敌人，模拟龙卷风下沉气流
    /// target 参数在此技能中不使用（技能以自身为中心触发）
    ///
    /// 修复历史：
    /// - 修复原版 null map 错误：在 MakeFlyer 前缓存地图和坐标引用
    /// - 修复 IReadOnlyList 转 List 的编译错误：改用索引器遍历
    /// </summary>
    public class CompAbilityEffect_TenguGale : CompAbilityEffect
    {
        public new CompProperties_AbilityTenguGale Props =>
            (CompProperties_AbilityTenguGale)this.props;

        /// <summary>
        /// 技能触发主逻辑
        /// target 参数被忽略，始终以 parent.pawn（施法者）为中心
        /// </summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = this.parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            Map map = caster.Map;
            IntVec3 casterPos = caster.Position;

            // 收集半径内所有可被吹飞的敌对 Pawn
            // 先收集列表再处理，避免在迭代 AllPawnsSpawned 时发生集合修改
            List<Pawn> targets = this.CollectTargets(caster, map, casterPos);

            // 逐一对每个目标执行击退
            for (int i = 0; i < targets.Count; i++)
            {
                this.ApplyKnockback(caster, targets[i], map);
            }
        }

        /// <summary>
        /// 收集半径内所有符合条件的敌对 Pawn
        /// 使用 IReadOnlyList 接口遍历，与 AllPawnsSpawned 的实际返回类型匹配
        /// </summary>
        private List<Pawn> CollectTargets(Pawn caster, Map map, IntVec3 center)
        {
            List<Pawn> result = new List<Pawn>();

            // AllPawnsSpawned 在1.6返回 IReadOnlyList<Pawn>，使用索引器遍历
            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn p = allPawns[i];

                // 逐一过滤：跳过自身、死亡/倒地、非敌对、体型过大、已在飞行中的目标
                if (p == caster) continue;
                if (p.Dead || p.Downed) continue;
                if (!p.HostileTo(caster)) continue;
                if (p.BodySize > this.Props.maxBodySize) continue;
                // 检查是否已经在某个 PawnFlyer 中，防止重复吹飞导致状态混乱
                if (p.ParentHolder is PawnFlyer) continue;
                // 距离检查（InHorDistOf 比 DistanceTo 效率更高）
                if (!p.Position.InHorDistOf(center, this.Props.blastRadius)) continue;

                result.Add(p);
            }

            return result;
        }

        /// <summary>
        /// 对单个目标执行击退飞行
        /// 关键：MakeFlyer 会 DeSpawn victim（victim.Map 变为 null），
        /// 因此所有引用必须在 MakeFlyer 调用前完成缓存
        /// </summary>
        private void ApplyKnockback(Pawn caster, Pawn victim, Map map)
        {
            // === 关键：在 MakeFlyer 前缓存所有需要的值 ===
            IntVec3 victimPosBeforeFlyer = victim.Position;
            // 不使用 victim.Map（MakeFlyer 后变 null），使用传入的 map 参数

            // 计算击退方向（从施法者指向目标）
            Vector3 pushDir = victim.DrawPos - caster.DrawPos;
            // 处理施法者和目标位置完全重叠的极端情况（给一个随机方向）
            if (pushDir.sqrMagnitude < 0.01f)
            {
                pushDir = new Vector3(Rand.Range(-1f, 1f), 0f, Rand.Range(-1f, 1f));
            }
            pushDir = pushDir.normalized;

            // 计算目标落点
            Vector3 destVec = victim.DrawPos + pushDir * this.Props.knockbackDistance;
            IntVec3 destCell = destVec.ToIntVec3();

            // 限制在地图边界内（留出3格缓冲，防止吹到地图边缘）
            destCell = this.ClampToMap(destCell, map, 3);

            // 寻找最近的可站立落点（处理落点在墙壁内的情况）
            destCell = this.FindNearestStandable(destCell, map, victimPosBeforeFlyer);

            if (this.Props.flyerDef == null) return;

            // 创建飞行器（此操作会 DeSpawn victim，之后 victim.Map == null）
            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                this.Props.flyerDef,
                victim,
                destCell,
                null,  // 飞行特效（由 XML 的 Effecter 处理）
                null   // 落地音效
            );

            if (flyer == null) return;

            // === 关键：使用缓存的 victimPosBeforeFlyer 和 map，而非 victim.Position/victim.Map ===
            GenSpawn.Spawn(flyer, victimPosBeforeFlyer, map, WipeMode.Vanish);
        }

        /// <summary>
        /// 将格子坐标限制在地图边界内
        /// </summary>
        /// <param name="cell">目标坐标</param>
        /// <param name="map">当前地图</param>
        /// <param name="margin">边界缓冲格数</param>
        private IntVec3 ClampToMap(IntVec3 cell, Map map, int margin)
        {
            if (map == null) return cell;
            int x = Mathf.Clamp(cell.x, margin, map.Size.x - margin - 1);
            int z = Mathf.Clamp(cell.z, margin, map.Size.z - margin - 1);
            return new IntVec3(x, cell.y, z);
        }

        /// <summary>
        /// 在指定位置附近寻找最近的可站立格子
        /// </summary>
        /// <param name="cell">期望落点</param>
        /// <param name="map">当前地图</param>
        /// <param name="fallback">如果找不到可站立点时的备用位置（通常是击退前的位置）</param>
        private IntVec3 FindNearestStandable(IntVec3 cell, Map map, IntVec3 fallback)
        {
            if (map == null) return fallback;
            // 如果期望落点本身可站立，直接使用
            if (cell.InBounds(map) && cell.Standable(map)) return cell;

            // 在5格范围内寻找最近的可站立格子
            IntVec3 result = CellFinder.StandableCellNear(cell, map, 5f);
            if (result.IsValid) return result;

            // 最终备用：返回击退前的原始位置
            return fallback;
        }

        /// <summary>
        /// 绘制技能预览：以施法者为中心显示吹飞范围圆圈
        /// 在玩家悬停技能按钮时调用
        /// </summary>
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (this.parent?.pawn == null || !this.parent.pawn.Spawned) return;
            // 青色圆圈标识天狗颪的覆盖范围
            GenDraw.DrawRadiusRing(
                this.parent.pawn.Position,
                this.Props.blastRadius,
                Color.cyan
            );
        }
    }
}