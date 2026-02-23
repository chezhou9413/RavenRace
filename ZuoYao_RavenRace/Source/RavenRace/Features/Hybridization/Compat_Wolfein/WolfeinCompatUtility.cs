using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Wolfein
{
    [StaticConstructorOnStartup]
    public static class WolfeinCompatUtility
    {
        public static bool IsWolfeinActive { get; private set; }
        public static HediffDef WolfeinBloodlineHediff { get; private set; }

        static WolfeinCompatUtility()
        {
            IsWolfeinActive = ModsConfig.IsActive("MelonDove.WolfeinRace");

            if (IsWolfeinActive)
            {
                WolfeinBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_WolfeinBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Wolfein mod detected. Compatibility active.");
            }
        }

        // [恢复]：供外部组件和 Harmony 补丁安全调用的 API
        public static bool HasWolfeinBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            var comp = pawn.TryGetComp<CompBloodline>();
            return BloodlineUtility.HasBloodline(comp, "Wolfein_Race");
        }
    }
}