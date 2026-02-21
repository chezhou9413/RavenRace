using System;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.GoldenGloria
{
    [StaticConstructorOnStartup]
    public static class GoldenGloriaCompatUtility
    {
        public static bool IsGoldenGloriaActive { get; private set; }
        public static ThingDef GoldenGloriaRaceDef { get; private set; }
        public static HediffDef GoldenGloriaGenotypeHediff { get; private set; }

        static GoldenGloriaCompatUtility()
        {
            // 根据 Race_GGl.xml 中的 defName="GoldenGlorias"
            GoldenGloriaRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("GoldenGlorias");
            IsGoldenGloriaActive = (GoldenGloriaRaceDef != null);

            // 根据 GGl_Gene.xml 中的 defName="GGl_GoldenGloria_Genotype"
            GoldenGloriaGenotypeHediff = DefDatabase<HediffDef>.GetNamedSilentFail("GGl_GoldenGloria_Genotype");

            if (IsGoldenGloriaActive)
            {
                RavenModUtility.LogVerbose("[RavenRace] Golden Gloria (GGl) detected. Compatibility active.");
            }
        }

        /// <summary>
        /// 检查是否拥有煌金族血脉
        /// </summary>
        public static bool HasGoldenGloriaBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            // 检查 Key 是否为 "GoldenGlorias" 且浓度 > 0
            return comp.BloodlineComposition.ContainsKey("GoldenGlorias") &&
                   comp.BloodlineComposition["GoldenGlorias"] > 0f;
        }

        /// <summary>
        /// 处理煌金族血脉带来的 Hediff (煌金基因型)
        /// </summary>
        public static void HandleGoldenGloriaBloodline(Pawn pawn, bool hasBloodline)
        {
            // 如果 Hediff 未找到（可能 Mod 版本不对），直接跳过
            if (pawn == null || GoldenGloriaGenotypeHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(GoldenGloriaGenotypeHediff);

            // 如果有血脉但没有 Hediff -> 添加
            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(GoldenGloriaGenotypeHediff);
            }
            // 如果没有血脉但有 Hediff -> 移除
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(GoldenGloriaGenotypeHediff);
                if (h != null)
                {
                    pawn.health.RemoveHediff(h);
                }
            }
        }
    }
}