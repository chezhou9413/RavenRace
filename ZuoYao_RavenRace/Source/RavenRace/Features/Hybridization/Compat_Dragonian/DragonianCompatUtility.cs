using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Dragonian
{
    [StaticConstructorOnStartup]
    public static class DragonianCompatUtility
    {
        public static bool IsDragonianActive { get; private set; }
        public static HediffDef DragonianBloodlineHediff { get; private set; }

        static DragonianCompatUtility()
        {
            IsDragonianActive = ModsConfig.IsActive("RooAndGloomy.DragonianRaceMod");

            if (IsDragonianActive)
            {
                DragonianBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_DragonianBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Dragonian detected. Compatibility active.");
            }
        }


    }
}