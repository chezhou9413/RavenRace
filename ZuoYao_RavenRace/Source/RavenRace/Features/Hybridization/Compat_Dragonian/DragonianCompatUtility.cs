using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Dragonian
{
    [StaticConstructorOnStartup]
    public static class DragonianCompatUtility
    {
        public static bool IsDragonianActive { get; private set; }
        public static HediffDef DragonianBloodlineHediff { get; private set; }

        static DragonianCompatUtility()
        {
            IsDragonianActive = ModsConfig.IsActive("RooAndGloomy.DragonianRaceMod");

            if (IsDragonianActive)
            {
                DragonianBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_DragonianBloodline");
                RavenModUtility.LogVerbose("[RavenRace] Dragonian detected. Compatibility active.");
            }
        }

        public static bool HasDragonianBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("Dragonian_Race") &&
                   comp.BloodlineComposition["Dragonian_Race"] > 0f;
        }

        public static void HandleDragonianBuff(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || pawn.health == null || DragonianBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(DragonianBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(DragonianBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(DragonianBloodlineHediff);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }
    }
}