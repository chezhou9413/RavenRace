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

        public static bool HasWolfeinBloodline(Pawn pawn)
        {
            var comp = pawn?.TryGetComp<CompBloodline>();
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("Wolfein_Race") &&
                   comp.BloodlineComposition["Wolfein_Race"] > 0f;
        }

        public static void HandleWolfeinBloodline(Pawn pawn, bool hasBloodline)
        {
            if (!IsWolfeinActive || pawn == null || WolfeinBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(WolfeinBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(WolfeinBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(WolfeinBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
            // 【核心修正】：删除了所有对 Wolfein.CompWolfeinStrength 的 AccessTools 反射操作。
            // 组件的初始化与否完全交由 XML 和 Patch_WolfeinStrength 拦截器处理，保障底层稳定。
        }
    }
}