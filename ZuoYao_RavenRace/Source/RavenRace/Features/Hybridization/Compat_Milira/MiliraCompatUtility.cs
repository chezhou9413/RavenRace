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


    }
}