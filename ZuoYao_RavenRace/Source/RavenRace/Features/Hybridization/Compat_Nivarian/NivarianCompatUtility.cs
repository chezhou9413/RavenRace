using System;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Nivarian
{
    /// <summary>
    /// 涅瓦莲 (Nivarian) 兼容性工具类
    /// </summary>
    [StaticConstructorOnStartup]
    public static class NivarianCompatUtility
    {
        public static bool IsNivarianActive { get; private set; }

        // 引用涅瓦莲的 Def
        public static ThingDef NivarianRaceDef { get; private set; }
        public static HediffDef UnyieldingFocusDef { get; private set; } // 原版的不屈之心 Hediff
        public static ThingDef MoteRisingDef { get; private set; }       // 上升粒子
        public static ThingDef MoteDecreasingDef { get; private set; }   // 下降粒子

        // 渡鸦自己的血脉标记 Hediff
        public static HediffDef RavenNivarianBloodlineHediff { get; private set; }

        static NivarianCompatUtility()
        {
            // 检测种族 Def 是否存在
            NivarianRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("NivarianRace_Pawn");
            IsNivarianActive = (NivarianRaceDef != null);

            if (IsNivarianActive)
            {
                // 获取原版 Def
                UnyieldingFocusDef = DefDatabase<HediffDef>.GetNamedSilentFail("Nivarian_Hediff_UnyieldingFocus");
                MoteRisingDef = DefDatabase<ThingDef>.GetNamedSilentFail("Nivarian_Mote_UnyieldingFocus_Rising");
                MoteDecreasingDef = DefDatabase<ThingDef>.GetNamedSilentFail("Nivarian_Mote_UnyieldingFocus_Decreasing");

                // 获取我们自己的 Def
                RavenNivarianBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_NivarianBloodline");

                RavenModUtility.LogVerbose("[RavenRace] Nivarian Race detected. Compatibility active.");
            }
        }

        /// <summary>
        /// 检查是否拥有涅瓦莲血脉
        /// </summary>
        public static bool HasNivarianBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            // 只要有 > 0 的涅瓦莲血脉即可
            return comp.BloodlineComposition.ContainsKey("NivarianRace_Pawn") &&
                   comp.BloodlineComposition["NivarianRace_Pawn"] > 0f;
        }

        /// <summary>
        /// 处理血脉状态同步
        /// 给拥有血脉的渡鸦添加 "Raven_Hediff_NivarianBloodline"，这个 Hediff 会带有模拟基因逻辑的组件
        /// </summary>
        public static void HandleNivarianBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || RavenNivarianBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(RavenNivarianBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(RavenNivarianBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(RavenNivarianBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);

                // 同时移除原版的不屈之心效果，防止残留
                if (UnyieldingFocusDef != null)
                {
                    Hediff focus = pawn.health.hediffSet.GetFirstHediffOfDef(UnyieldingFocusDef);
                    if (focus != null) pawn.health.RemoveHediff(focus);
                }
            }
        }
    }
}