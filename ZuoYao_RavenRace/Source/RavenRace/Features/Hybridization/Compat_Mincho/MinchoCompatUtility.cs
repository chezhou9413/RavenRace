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

        // [恢复]：供外部组件和 Harmony 补丁安全调用的 API
        public static bool HasMinchoBloodline(CompBloodline comp)
        {
            return BloodlineUtility.HasBloodline(comp, "Mincho_ThingDef");
        }

        public static bool HasMinchoBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            return HasMinchoBloodline(pawn.TryGetComp<CompBloodline>());
        }
    }
}