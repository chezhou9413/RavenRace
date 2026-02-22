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

        public static bool HasCinderBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("Alien_Cinder") &&
                   comp.BloodlineComposition["Alien_Cinder"] > 0f;
        }

        public static void HandleCinderRegen(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || CinderBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(CinderBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(CinderBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(CinderBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }
    }
}