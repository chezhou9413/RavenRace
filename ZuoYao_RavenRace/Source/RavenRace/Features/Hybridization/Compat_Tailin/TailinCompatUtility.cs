using System;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Tailin
{
    [StaticConstructorOnStartup]
    public static class TailinCompatUtility
    {
        public static bool IsTailinActive { get; private set; }
        public static HediffDef TailinBloodlineHediff { get; private set; }

        static TailinCompatUtility()
        {
            // 【核心规范】：不再硬猜 ThingDef，直接通过 PackageId 精准判定模组是否激活
            IsTailinActive = ModsConfig.IsActive("LepechandEusro.Tailin");

            if (IsTailinActive)
            {
                TailinBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_TailinBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Tailin compatibility active.");

                // =========================================================================
                // 【核心防御】：修复 CXCore/Tailin 动态 Def 缺失哈希值的 Bug
                // 手动为哈希值为 0 的投射物分配安全哈希，完美避开读档 short hash 0 崩溃。
                // =========================================================================
                FixCXCoreShortHashBug();
            }
        }

        private static void FixCXCoreShortHashBug()
        {
            int fixedCount = 0;
            ushort manualHash = 60000; // 从高位开始分配，极大概率避免和原版及其他Mod冲突

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.shortHash == 0 && !string.IsNullOrEmpty(def.defName))
                {
                    def.shortHash = manualHash++;
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                Log.Message($"[RavenRace] 成功为 {fixedCount} 个动态生成的 Def 分配了临时哈希值 (防御 CXCore/Tailin 底层 Bug)。");
            }
        }

        public static bool HasTailinBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            // 对应 Bloodline_Tailin.xml 中的 <raceDef>TailinRace</raceDef>
            return comp.BloodlineComposition.ContainsKey("TailinRace") && comp.BloodlineComposition["TailinRace"] > 0f;
        }

        public static void HandleTailinBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || TailinBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(TailinBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(TailinBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TailinBloodlineHediff);
                if (hediff != null) pawn.health.RemoveHediff(hediff);
            }
        }
    }
}