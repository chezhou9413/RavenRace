using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.Hediffs
{
    /// <summary>
    /// 黄金精神：动态插值 Hediff
    /// </summary>
    public class Hediff_GoldenSpirit : HediffWithComps
    {
        // 缓存当前的 Stage，避免每帧 new 对象
        private HediffStage curStage;

        // 记录上一次更新时的 Severity，用于判断是否需要更新
        private float lastSeverity = -1f;

        // 配置参数：最大属性加成 (100% 时)
        private const float MaxMoveSpeed = 8.0f;       // 移速 +8
        private const float MaxMeleeHit = 100f;        // 命中 +100
        private const float MaxMeleeDodge = 100f;      // 闪避 +100
        private const float MaxDamageFactor = 10.0f;   // 伤害 x10
        private const float MinIncomingDamage = 0.05f; // 承伤 x0.05 (95% 减免)
        private const float MinMeleeCooldown = 0.2f;   // 攻速 x5 (冷却缩减至 0.2)

        public override HediffStage CurStage
        {
            get
            {
                // 如果 Severity 发生显著变化，或者是第一次访问，则重建 Stage
                if (curStage == null || Mathf.Abs(this.Severity - lastSeverity) > 0.0001f)
                {
                    UpdateStage();
                }
                return curStage;
            }
        }

        private void UpdateStage()
        {
            lastSeverity = this.Severity;
            float p = this.Severity; // 0.0 ~ 1.0

            if (curStage == null) curStage = new HediffStage();

            // --- 动态计算数值 ---

            // 初始化列表 (如果为空)
            if (curStage.statOffsets == null) curStage.statOffsets = new List<StatModifier>();
            if (curStage.statFactors == null) curStage.statFactors = new List<StatModifier>();
            if (curStage.capMods == null) curStage.capMods = new List<PawnCapacityModifier>();

            curStage.statOffsets.Clear();
            curStage.statFactors.Clear();
            curStage.capMods.Clear();

            // 1. 基础线性插值属性 (Offset)
            curStage.statOffsets.Add(new StatModifier { stat = StatDefOf.MoveSpeed, value = Mathf.Lerp(0f, MaxMoveSpeed, p) });
            curStage.statOffsets.Add(new StatModifier { stat = StatDefOf.MeleeHitChance, value = Mathf.Lerp(0f, MaxMeleeHit, p) });
            curStage.statOffsets.Add(new StatModifier { stat = StatDefOf.MeleeDodgeChance, value = Mathf.Lerp(0f, MaxMeleeDodge, p) });

            // 2. 乘区属性 (Factor)
            curStage.statFactors.Add(new StatModifier { stat = StatDefOf.MeleeDamageFactor, value = Mathf.Lerp(1.0f, MaxDamageFactor, p) });
            curStage.statFactors.Add(new StatModifier { stat = StatDefOf.IncomingDamageFactor, value = Mathf.Lerp(1.0f, MinIncomingDamage, p) });

            // [新增] 攻速加成 (冷却缩减)
            curStage.statFactors.Add(new StatModifier { stat = StatDefOf.MeleeWeapon_CooldownMultiplier, value = Mathf.Lerp(1.0f, MinMeleeCooldown, p) });

            // 3. 关键阈值奖励

            // 50% 阈值：强力回血
            if (p >= 0.5f)
            {
                curStage.totalBleedFactor = 0.1f;
                curStage.naturalHealingFactor = 10.0f; // 提升回血速度
            }
            else
            {
                curStage.totalBleedFactor = 1f;
                curStage.naturalHealingFactor = 1f;
            }

            // 100% 阈值：黄金体验 (无痛，免疫流血)
            if (p >= 1.0f)
            {
                curStage.totalBleedFactor = 0f;
                curStage.painFactor = 0f;
                curStage.naturalHealingFactor = 50.0f; // 极速再生

                curStage.label = "黄金体验 (MAX)";
            }
            else
            {
                curStage.painFactor = 1f;
                curStage.label = $"同步率 {p.ToStringPercent("F2")}";
            }
        }

        public override void PostTick()
        {
            base.PostTick();
            // 确保 Severity 不会因为原版逻辑衰减
            if (this.Severity > 0 && this.ageTicks % 60 == 0)
            {
                // HediffComp_SeverityPerDay(0) 应该已经处理了
            }
        }
    }
}