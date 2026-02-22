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

        public static bool HasNemesisBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey(NemesisKey) &&
                   comp.BloodlineComposition[NemesisKey] > 0f;
        }

        public static void HandleNemesisBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || NemesisBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(NemesisBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(NemesisBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(NemesisBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }
    }
}