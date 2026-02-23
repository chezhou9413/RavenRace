using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Nivarian
{
    [StaticConstructorOnStartup]
    public static class NivarianCompatUtility
    {
        public static bool IsNivarianActive { get; private set; }

        public static HediffDef UnyieldingFocusDef { get; private set; }
        public static ThingDef MoteRisingDef { get; private set; }
        public static ThingDef MoteDecreasingDef { get; private set; }

        public static HediffDef RavenNivarianBloodlineHediff { get; private set; }

        static NivarianCompatUtility()
        {
            IsNivarianActive = ModsConfig.IsActive("keeptpa.NivarianRace");

            if (IsNivarianActive)
            {
                UnyieldingFocusDef = DefDatabase<HediffDef>.GetNamedSilentFail("Nivarian_Hediff_UnyieldingFocus");
                MoteRisingDef = DefDatabase<ThingDef>.GetNamedSilentFail("Nivarian_Mote_UnyieldingFocus_Rising");
                MoteDecreasingDef = DefDatabase<ThingDef>.GetNamedSilentFail("Nivarian_Mote_UnyieldingFocus_Decreasing");
                RavenNivarianBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_NivarianBloodline");

                RavenModUtility.LogVerbose("[RavenRace] Nivarian Race detected. Compatibility active.");
            }
        }


    }
}