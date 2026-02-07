using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline; // [Added]

namespace RavenRace.Compat.Epona
{
    [StaticConstructorOnStartup]
    public static class EponaCompatUtility
    {
        public static bool IsEponaActive { get; private set; }
        public static HediffDef EponaRunHediff { get; private set; }

        static EponaCompatUtility()
        {
            IsEponaActive = DefDatabase<ThingDef>.GetNamedSilentFail("Alien_Epona") != null ||
                            DefDatabase<ThingDef>.GetNamedSilentFail("Alien_Destrier") != null ||
                            DefDatabase<ThingDef>.GetNamedSilentFail("Alien_Unicorn") != null;

            if (IsEponaActive)
            {
                EponaRunHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Epona_Run_Hediff");
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
            // [Change] Comp_Bloodline -> CompBloodline
            var comp = pawn.TryGetComp<CompBloodline>();
            if (comp == null || comp.BloodlineComposition == null) return false;

            return comp.BloodlineComposition.ContainsKey("Alien_Epona") ||
                   comp.BloodlineComposition.ContainsKey("Alien_Destrier") ||
                   comp.BloodlineComposition.ContainsKey("Alien_Unicorn");
        }

        public static void EnsureEponaHybridComp(Pawn pawn)
        {
            if (!IsEponaActive || pawn == null) return;
            if (!HasEponaBloodline(pawn)) return;

            if (pawn.GetComp<Comp_EponaHybridLogic>() != null) return;

            try
            {
                Comp_EponaHybridLogic newComp = new Comp_EponaHybridLogic();
                newComp.parent = pawn;
                newComp.Initialize(new CompProperties_EponaHybridLogic());
                pawn.AllComps.Add(newComp);
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Failed to add EponaHybridLogic to {pawn}: {ex}");
            }
        }
    }
}