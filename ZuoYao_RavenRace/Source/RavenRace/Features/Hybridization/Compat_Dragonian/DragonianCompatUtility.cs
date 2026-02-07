using System;
using System.Linq;
using Verse;
using RimWorld;
using RavenRace.Compat.MuGirl;
using RavenRace.Features.Bloodline; // [Added]

namespace RavenRace.Compat.Dragonian
{
    [StaticConstructorOnStartup]
    public static class DragonianCompatUtility
    {
        public static bool IsDragonianActive { get; private set; }
        public static ThingDef DragonianRaceDef { get; private set; }
        public static ThingDef DragonianMilkDef { get; private set; }
        public static ThingDef DivineMilkDef { get; private set; }
        public static HediffDef DragonianBloodlineHediff { get; private set; }

        static DragonianCompatUtility()
        {
            DragonianRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("Dragonian_Race");
            IsDragonianActive = (DragonianRaceDef != null);
            DragonianMilkDef = DefDatabase<ThingDef>.GetNamedSilentFail("DragonianMilk");
            DivineMilkDef = DefDatabase<ThingDef>.GetNamedSilentFail("Raven_DivineMilk");
            DragonianBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_DragonianBloodline");

            if (IsDragonianActive)
            {
                RavenModUtility.LogVerbose("[RavenRace] Dragonian detected. Compatibility active.");
            }
        }

        // [Change] Comp_Bloodline -> CompBloodline
        public static bool HasDragonianBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("Dragonian_Race") &&
                   comp.BloodlineComposition["Dragonian_Race"] > 0f;
        }

        public static void HandleDragonianBuff(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || DragonianBloodlineHediff == null) return;

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

        public static void GrantDragonMilkAbility(Pawn pawn)
        {
            ThingDef product = DragonianMilkDef ?? DefDatabase<ThingDef>.GetNamed("Milk");
            AddMilkComp(pawn, product, 10, 2.5f, "RavenRace_DragonMilkFullness");
        }

        public static void GrantDivineMilkAbility(Pawn pawn)
        {
            ThingDef product = DivineMilkDef ?? DragonianMilkDef ?? DefDatabase<ThingDef>.GetNamed("Milk");
            AddMilkComp(pawn, product, 15, 2.0f, "RavenRace_DivineMilkFullness");
        }

        private static void AddMilkComp(Pawn pawn, ThingDef product, int amount, float interval, string labelKey)
        {
            try
            {
                CompProperties_RavenMilkable props = new CompProperties_RavenMilkable
                {
                    milkDef = product,
                    milkAmount = amount,
                    milkIntervalDays = interval,
                    displayStringKey = labelKey
                };

                CompRavenMilkable newComp = new CompRavenMilkable();
                newComp.parent = pawn;
                newComp.Initialize(props);
                pawn.AllComps.Add(newComp);
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Failed to grant milk ability ({product}): {ex}");
            }
        }
    }
}