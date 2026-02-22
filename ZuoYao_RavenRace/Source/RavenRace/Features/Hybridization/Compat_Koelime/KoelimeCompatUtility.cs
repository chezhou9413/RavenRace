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

        public static void HandleDraconicBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || KoelimeBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(KoelimeBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(KoelimeBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(KoelimeBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }
    }
}