using System;
using System.Linq;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline; // [Added]

namespace RavenRace.Compat.MuGirl
{
    [StaticConstructorOnStartup]
    public static class MuGirlCompatUtility
    {
        public static bool IsMuGirlActive { get; private set; }

        public static ThingDef MuGirlRaceDef { get; private set; }
        public static ThingDef MuGirlMilkDef { get; private set; }
        public static HediffDef MuGirlBloodlineHediff { get; private set; }

        public static HediffDef MooGirl_Charge { get; private set; }
        public static HediffDef MooGirl_Stun { get; private set; }
        public static AbilityDef RavenChargeAbility { get; private set; }

        static MuGirlCompatUtility()
        {
            MuGirlRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("MooGirl");
            IsMuGirlActive = (MuGirlRaceDef != null);

            MuGirlMilkDef = DefDatabase<ThingDef>.GetNamedSilentFail("MooGirl_Milk");
            MuGirlBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MuGirlBloodline");

            MooGirl_Charge = DefDatabase<HediffDef>.GetNamedSilentFail("MooGirl_Charge");
            MooGirl_Stun = DefDatabase<HediffDef>.GetNamedSilentFail("MooGirl_Stun");

            RavenChargeAbility = DefDatabase<AbilityDef>.GetNamedSilentFail("Raven_Ability_MuGirlCharge");

            if (IsMuGirlActive)
            {
                RavenModUtility.LogVerbose("[RavenRace] MooGirl detected. Compatibility active.");
            }
        }

        public static void HandleMuGirlBloodline(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null) return;
            if (MuGirlBloodlineHediff != null)
            {
                bool hasHediff = pawn.health.hediffSet.HasHediff(MuGirlBloodlineHediff);
                if (hasBloodline && !hasHediff) pawn.health.AddHediff(MuGirlBloodlineHediff);
                else if (!hasBloodline && hasHediff)
                {
                    Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(MuGirlBloodlineHediff);
                    if (h != null) pawn.health.RemoveHediff(h);
                }
            }
        }

        public static void EnsureMilkable(Pawn pawn)
        {
            if (pawn.TryGetComp<CompRavenMilkable>() != null) return;
            if (pawn.AllComps.Any(c => c.GetType().Name == "CompMooMilkable")) return;

            ThingDef product = MuGirlMilkDef ?? DefDatabase<ThingDef>.GetNamed("Milk");
            try
            {
                CompProperties_RavenMilkable props = new CompProperties_RavenMilkable
                {
                    milkDef = product,
                    milkAmount = 11,
                    milkIntervalDays = 1f,
                    displayStringKey = "RavenRace_MilkFullness"
                };
                CompRavenMilkable newComp = new CompRavenMilkable();
                newComp.parent = pawn;
                newComp.Initialize(props);
                pawn.AllComps.Add(newComp);
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Failed to grant milkable ability to {pawn.LabelShort}: {ex}");
            }
        }

        // [Change] Comp_Bloodline -> CompBloodline
        public static bool HasMuGirlBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("MooGirl") && comp.BloodlineComposition["MooGirl"] > 0f;
        }

        public static void HandleChargeAbility(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || RavenChargeAbility == null) return;

            bool hasAbility = pawn.abilities.GetAbility(RavenChargeAbility) != null;

            if (hasBloodline && !hasAbility)
            {
                pawn.abilities.GainAbility(RavenChargeAbility);
            }
            else if (!hasBloodline && hasAbility)
            {
                pawn.abilities.RemoveAbility(RavenChargeAbility);
            }
        }
    }
}