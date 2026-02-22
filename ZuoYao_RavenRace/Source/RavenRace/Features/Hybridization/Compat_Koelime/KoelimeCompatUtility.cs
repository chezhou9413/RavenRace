using Verse;
using RimWorld;

namespace RavenRace.Compat.Koelime
{
    [StaticConstructorOnStartup]
    public static class KoelimeCompatUtility
    {
        public static bool IsKoelimeActive { get; private set; }
        public static HediffDef KoelimeBloodlineHediff { get; private set; }

        static KoelimeCompatUtility()
        {
            IsKoelimeActive = ModsConfig.IsActive("Draconis.Koelime");

            if (IsKoelimeActive)
            {
                KoelimeBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_KoelimeBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Koelime detected. Compatibility active.");
            }
        }


    }
}