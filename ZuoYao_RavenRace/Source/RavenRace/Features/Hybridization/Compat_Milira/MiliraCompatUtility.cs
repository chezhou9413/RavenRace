using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Milira
{
    [StaticConstructorOnStartup]
    public static class MiliraCompatUtility
    {
        public static bool IsMiliraActive { get; private set; }
        public static ThingDef MiliraRaceDef { get; private set; }
        public static HediffDef MiliraBloodlineHediff { get; private set; }

        static MiliraCompatUtility()
        {
            // 尝试查找米莉拉种族 Def
            MiliraRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("Milira_Race");

            // 如果没找到 Milira_Race，尝试找 Milira (以防版本差异)
            if (MiliraRaceDef == null)
            {
                MiliraRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("Milira");
            }

            IsMiliraActive = (MiliraRaceDef != null);

            // 获取我们新定义的 Hediff
            MiliraBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MiliraBloodline");

            if (IsMiliraActive)
            {
                RavenModUtility.LogVerbose("[RavenRace] Milira detected. Compatibility active.");
            }
        }

        /// <summary>
        /// 检查是否拥有米莉拉血脉
        /// </summary>
        public static bool HasMiliraBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;

            // 检查可能的 Key
            return comp.BloodlineComposition.ContainsKey("Milira_Race") ||
                   comp.BloodlineComposition.ContainsKey("Milira");
        }

        /// <summary>
        /// 处理米莉拉血脉带来的属性加成 Hediff
        /// </summary>
        public static void HandleMiliraBuff(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || MiliraBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(MiliraBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(MiliraBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(MiliraBloodlineHediff);
                if (h != null)
                {
                    pawn.health.RemoveHediff(h);
                }
            }
        }
    }
}