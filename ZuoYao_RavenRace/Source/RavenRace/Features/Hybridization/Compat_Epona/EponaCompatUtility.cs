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

        // [恢复]：供外部组件和 Harmony 补丁安全调用的 API
        public static bool HasEponaBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            var comp = pawn.TryGetComp<CompBloodline>();
            return BloodlineUtility.HasBloodline(comp, "Alien_Epona", "Alien_Destrier", "Alien_Unicorn");
        }
    }
}