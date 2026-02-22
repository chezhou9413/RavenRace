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

        public static bool HasGoldenGloriaBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("GoldenGlorias") &&
                   comp.BloodlineComposition["GoldenGlorias"] > 0f;
        }

        public static void HandleGoldenGloriaBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || GoldenGloriaGenotypeHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(GoldenGloriaGenotypeHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(GoldenGloriaGenotypeHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(GoldenGloriaGenotypeHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }
    }
}