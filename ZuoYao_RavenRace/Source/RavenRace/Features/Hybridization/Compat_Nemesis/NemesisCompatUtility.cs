using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Nemesis
{
    [StaticConstructorOnStartup]
    public static class NemesisCompatUtility
    {
        public static bool IsNemesisActive { get; private set; }
        public static HediffDef NemesisBloodlineHediff { get; private set; }
        public static FleckDef NemesisTeleportFleck { get; private set; }

        public const string NemesisKey = "Nemesis_Race";

        static NemesisCompatUtility()
        {
            IsNemesisActive = ModsConfig.IsActive("Aurora.Nebula.NemesisRaceThePunisher");

            if (IsNemesisActive)
            {
                NemesisBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_NemesisBloodline");
                NemesisTeleportFleck = DefDatabase<FleckDef>.GetNamedSilentFail("Nemesis_Fleck_Anim1");

                RavenModUtility.LogVerbose("[RavenRace] Nemesis compatibility active.");
            }
        }


    }
}