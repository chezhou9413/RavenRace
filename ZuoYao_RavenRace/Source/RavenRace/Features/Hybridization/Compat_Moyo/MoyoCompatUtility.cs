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

        // [恢复]：供外部组件和 Harmony 补丁安全调用的 API
        public static bool HasMoyoBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            var comp = pawn.TryGetComp<CompBloodline>();
            return BloodlineUtility.HasBloodline(comp, "Alien_Moyo");
        }
    }
}