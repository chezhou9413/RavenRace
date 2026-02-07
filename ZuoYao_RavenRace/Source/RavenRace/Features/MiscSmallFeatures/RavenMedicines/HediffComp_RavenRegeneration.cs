using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.RavenMedicines
{
    /// <summary>
    /// 渡鸦栓剂的再生组件
    /// 功能：每隔一段时间随机治疗身上的一个伤口，并提高免疫性。
    /// </summary>
    public class CompProperties_RavenRegeneration : HediffCompProperties
    {
        // [用户指定] 极度强力的数值设定
        public int healIntervalTicks = 60; // 每1秒(60tick)治疗一次
        public float healAmount = 10.0f;   // 每次治疗10点 (极快，基本瞬间满血)
        public float immunityOffset = 10f; // 免疫增益 (注意：此数值主要供C#逻辑参考，实际免疫效果需在XML的statOffsets中配置)

        public CompProperties_RavenRegeneration()
        {
            this.compClass = typeof(HediffComp_RavenRegeneration);
        }
    }

    public class HediffComp_RavenRegeneration : HediffComp
    {
        public CompProperties_RavenRegeneration Props => (CompProperties_RavenRegeneration)props;

        private int ticksCounter = 0;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            ticksCounter++;

            if (ticksCounter >= Props.healIntervalTicks)
            {
                ticksCounter = 0;
                TryHealRandomInjury();
            }
        }

        private void TryHealRandomInjury()
        {
            // [修复 CS1061] Pawn_HealthTracker 没有 HasHediffs 属性
            // 正确做法是检查 hediffSet.hediffs 列表是否有内容
            if (Pawn == null || Pawn.Dead || Pawn.health.hediffSet.hediffs.Count == 0) return;

            // 查找所有非永久性的损伤 (Hediff_Injury)
            // 排除掉已经愈合的或者不可治疗的
            List<Hediff_Injury> injuries = Pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(h => h.CanHealNaturally() || h.CanHealFromTending())
                .ToList();

            if (injuries.Count > 0)
            {
                Hediff_Injury injuryToHeal = injuries.RandomElement();

                // 执行治疗
                injuryToHeal.Heal(Props.healAmount);

                // [核心规范] 既然治疗了伤口（可能会消除伤疤或绷带视觉效果），标记渲染为脏以强制刷新
                Pawn.Drawer?.renderer?.SetAllGraphicsDirty();

                // [修复编译错误] 
                // 原代码使用了 MoteMaker.ThrowMetaIcon 和 ThingDefOf.Mote_HealingCross，
                // 这在 1.6/1.5+ 版本中已更改为 FleckMaker 和 FleckDef。
                // 为了遵循“不猜测API”的原则，确保编译通过，此处移除了可能导致报错的纯视觉特效代码。
                // 核心治疗逻辑不受影响。
            }
        }
    }
}