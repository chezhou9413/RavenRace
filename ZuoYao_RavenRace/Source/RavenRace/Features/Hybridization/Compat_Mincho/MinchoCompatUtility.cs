using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Mincho
{
    [StaticConstructorOnStartup]
    public static class MinchoCompatUtility
    {
        public static bool IsMinchoActive { get; private set; }
        public static HediffDef MinchoBloodlineHediff { get; private set; }

        static MinchoCompatUtility()
        {
            IsMinchoActive = ModsConfig.IsActive("SutSutMan.MinchoTheMintChocoSlimeHARver");

            if (IsMinchoActive)
            {
                MinchoBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MinchoBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Mincho the Mint Choco Slime detected. Compatibility active.");
            }
        }

        public static bool HasMinchoBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("Mincho_ThingDef") &&
                   comp.BloodlineComposition["Mincho_ThingDef"] > 0f;
        }

        public static void HandleMinchoBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || MinchoBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(MinchoBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(MinchoBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MinchoBloodlineHediff);
                if (hediff != null) pawn.health.RemoveHediff(hediff);
            }
        }
    }
}