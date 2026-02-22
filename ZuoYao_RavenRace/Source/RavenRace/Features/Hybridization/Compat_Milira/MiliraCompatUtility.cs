using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Milira
{
    /// <summary>
    /// 米莉拉兼容性工具类
    /// 负责检测模组状态并安全地处理血脉Hediff的赋予和移除
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MiliraCompatUtility
    {
        public static bool IsMiliraActive { get; private set; }
        public static ThingDef MiliraRaceDef { get; private set; }
        public static HediffDef MiliraBloodlineHediff { get; private set; }

        static MiliraCompatUtility()
        {
            // 尝试获取米莉拉种族 Def (兼容带 _Race 和不带后缀的版本)
            MiliraRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("Milira_Race")
                         ?? DefDatabase<ThingDef>.GetNamedSilentFail("Milira");

            IsMiliraActive = (MiliraRaceDef != null);

            if (IsMiliraActive)
            {
                // 获取血脉加成 Hediff
                MiliraBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MiliraBloodline");

                if (MiliraBloodlineHediff == null)
                {
                    Log.Warning("[RavenRace] Milira detected, but 'Raven_Hediff_MiliraBloodline' not found in XML.");
                }
                else
                {
                    RavenModUtility.LogVerbose("[RavenRace] Milira detected. Compatibility active.");
                }
            }
        }

        /// <summary>
        /// 检查血脉组件中是否含有米莉拉血脉（且占比大于0）
        /// </summary>
        public static bool HasMiliraBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;

            // 检查可能的 Key，只要大于 0 即视为拥有
            if (comp.BloodlineComposition.TryGetValue("Milira_Race", out float miliraRaceVal) && miliraRaceVal > 0f) return true;
            if (comp.BloodlineComposition.TryGetValue("Milira", out float miliraVal) && miliraVal > 0f) return true;

            return false;
        }

        /// <summary>
        /// 处理米莉拉血脉带来的属性加成 Hediff (天使血脉)
        /// 此方法由 CompBloodline.CheckAndGrantBloodlineAbilities 统一调用
        /// </summary>
        public static void HandleMiliraBuff(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || MiliraBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(MiliraBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(MiliraBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff hediffToRemove = pawn.health.hediffSet.GetFirstHediffOfDef(MiliraBloodlineHediff);
                if (hediffToRemove != null)
                {
                    pawn.health.RemoveHediff(hediffToRemove);
                }
            }
        }
    }
}