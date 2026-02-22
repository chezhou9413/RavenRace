using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.MuGirl
{
    [StaticConstructorOnStartup]
    public static class MuGirlCompatUtility
    {
        public static bool IsMuGirlActive { get; private set; }
        public static HediffDef MuGirlBloodlineHediff { get; private set; }
        public static HediffDef MooGirl_Charge { get; private set; }
        public static HediffDef MooGirl_Stun { get; private set; }
        public static AbilityDef RavenChargeAbility { get; private set; }

        static MuGirlCompatUtility()
        {
            IsMuGirlActive = ModsConfig.IsActive("HAR.MuGirlRace");

            if (IsMuGirlActive)
            {
                MuGirlBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MuGirlBloodline");
                MooGirl_Charge = DefDatabase<HediffDef>.GetNamedSilentFail("MooGirl_Charge");
                MooGirl_Stun = DefDatabase<HediffDef>.GetNamedSilentFail("MooGirl_Stun");
                RavenChargeAbility = DefDatabase<AbilityDef>.GetNamedSilentFail("Raven_Ability_MuGirlCharge");

                RavenModUtility.LogVerbose("[RavenRace] MooGirl detected. Compatibility active.");
            }
        }


        public static void HandleChargeAbility(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || RavenChargeAbility == null || pawn.abilities == null) return;

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