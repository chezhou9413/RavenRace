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

        public static bool HasNivarianBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("NivarianRace_Pawn") &&
                   comp.BloodlineComposition["NivarianRace_Pawn"] > 0f;
        }

        public static void HandleNivarianBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || RavenNivarianBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(RavenNivarianBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(RavenNivarianBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(RavenNivarianBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);

                if (UnyieldingFocusDef != null)
                {
                    Hediff focus = pawn.health.hediffSet.GetFirstHediffOfDef(UnyieldingFocusDef);
                    if (focus != null) pawn.health.RemoveHediff(focus);
                }
            }
        }
    }
}