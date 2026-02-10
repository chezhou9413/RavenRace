using Verse;
using RimWorld;

namespace RavenRace.Compat.Milira
{
    [StaticConstructorOnStartup]
    public static class MiliraCompatUtility
    {
        public static bool IsMiliraActive { get; private set; }

        static MiliraCompatUtility()
        {
            // 仅保留基础检测，用于判断血脉是否存在
            IsMiliraActive = DefDatabase<ThingDef>.GetNamedSilentFail("Milira_Race") != null
                             || DefDatabase<ThingDef>.GetNamedSilentFail("Milira") != null;
        }
    }
}