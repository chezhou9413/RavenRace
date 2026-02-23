using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Cinder
{
    [StaticConstructorOnStartup]
    public static class CinderCompatUtility
    {
        public static bool IsCinderActive { get; private set; }
        public static HediffDef CinderBloodlineHediff { get; private set; }

        static CinderCompatUtility()
        {
            IsCinderActive = ModsConfig.IsActive("BreadMo.Cinders");

            if (IsCinderActive)
            {
                CinderBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_CinderBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Cinder (Embergarden) detected. Compatibility active.");
            }
        }


    }
}