using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.GoldenGloria
{
    [StaticConstructorOnStartup]
    public static class GoldenGloriaCompatUtility
    {
        public static bool IsGoldenGloriaActive { get; private set; }
        public static HediffDef GoldenGloriaGenotypeHediff { get; private set; }

        static GoldenGloriaCompatUtility()
        {
            IsGoldenGloriaActive = ModsConfig.IsActive("Golden.GloriasMod");

            if (IsGoldenGloriaActive)
            {
                GoldenGloriaGenotypeHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_GoldenGloriaBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Golden Gloria detected. Compatibility active.");
            }
        }


    }
}