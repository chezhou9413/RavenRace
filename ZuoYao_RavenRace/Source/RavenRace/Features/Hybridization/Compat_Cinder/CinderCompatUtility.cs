using System;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Cinder
{
    [StaticConstructorOnStartup]
    public static class CinderCompatUtility
    {
        public static bool IsCinderActive { get; private set; }
        public static ThingDef CinderRaceDef { get; private set; }
        public static HediffDef CinderBloodlineHediff { get; private set; }

        static CinderCompatUtility()
        {
            // 根据 About.xml 中的 packageId 或 RaceDefName 判断
            CinderRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("Alien_Cinder");
            IsCinderActive = (CinderRaceDef != null);

            CinderBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_CinderBloodline");

            if (IsCinderActive)
            {
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
            if (pawn == null || CinderBloodlineHediff == null) return;

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