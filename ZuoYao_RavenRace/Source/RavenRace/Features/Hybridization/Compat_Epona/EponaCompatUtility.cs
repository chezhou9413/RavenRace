using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Epona
{
    [StaticConstructorOnStartup]
    public static class EponaCompatUtility
    {
        public static bool IsEponaActive { get; private set; }
        public static HediffDef EponaRunHediff { get; private set; }
        public static HediffDef EponaBloodlineHediff { get; private set; }

        static EponaCompatUtility()
        {
            IsEponaActive = ModsConfig.IsActive("Epona.EponaDynasticRise");

            if (IsEponaActive)
            {
                EponaRunHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Epona_Run_Hediff");
                EponaBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_EponaBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Epona detected. Compatibility active.");
            }
        }

        public static bool IsEponaRace(string defName)
        {
            if (defName == null) return false;
            return defName == "Alien_Epona" || defName == "Alien_Destrier" || defName == "Alien_Unicorn";
        }

        public static string NormalizeToEponaKey(string defName)
        {
            if (IsEponaRace(defName)) return "Alien_Epona";
            return defName;
        }

        public static bool HasEponaBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            var comp = pawn.TryGetComp<CompBloodline>();
            if (comp == null || comp.BloodlineComposition == null) return false;

            return comp.BloodlineComposition.ContainsKey("Alien_Epona") ||
                   comp.BloodlineComposition.ContainsKey("Alien_Destrier") ||
                   comp.BloodlineComposition.ContainsKey("Alien_Unicorn");
        }

        public static void HandleEponaBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || EponaBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(EponaBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(EponaBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(EponaBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }
    }
}