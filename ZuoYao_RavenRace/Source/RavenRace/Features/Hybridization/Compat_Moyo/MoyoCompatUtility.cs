using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Moyo
{
    [StaticConstructorOnStartup]
    public static class MoyoCompatUtility
    {
        public static bool IsMoyoActive { get; private set; }
        public static HediffDef MoyoBloodlineHediff { get; private set; }

        static MoyoCompatUtility()
        {
            IsMoyoActive = ModsConfig.IsActive("Nemonian.MY2.Beta");

            if (IsMoyoActive)
            {
                MoyoBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MoyoBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Moyo mod detected. Compatibility active.");
            }
        }

        public static bool HasMoyoBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            var comp = pawn.TryGetComp<CompBloodline>();
            if (comp != null && comp.BloodlineComposition != null)
            {
                return comp.BloodlineComposition.ContainsKey("Alien_Moyo") && comp.BloodlineComposition["Alien_Moyo"] > 0f;
            }
            return false;
        }

        public static void HandleMoyoBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || MoyoBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(MoyoBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(MoyoBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(MoyoBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }
    }
}