using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Linq;
using RavenRace.Features.Bloodline; // [Added]

namespace RavenRace.Compat.Moyo
{
    [StaticConstructorOnStartup]
    public static class MoyoCompatUtility
    {
        public static bool IsMoyoActive { get; private set; }

        public static ThingDef MoyoRaceDef;
        public static ThingDef BlueSerumDef;
        public static JobDef HarvestJobDef;

        public static Type CompResourceHarvestableType;
        public static Type CompProperties_ResourceHarvestableType;
        public static Type ConstraintPawnAgeType;

        static MoyoCompatUtility()
        {
            try
            {
                MoyoRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("Alien_Moyo");
                IsMoyoActive = (MoyoRaceDef != null);

                if (IsMoyoActive)
                {
                    BlueSerumDef = DefDatabase<ThingDef>.GetNamedSilentFail("BlueSerum");
                    HarvestJobDef = DefDatabase<JobDef>.GetNamedSilentFail("Moyo2_ExtractDeepBlue_Job");

                    CompResourceHarvestableType = AccessTools.TypeByName("Moyo2_HPF.CompResourceHarvestable");
                    CompProperties_ResourceHarvestableType = AccessTools.TypeByName("Moyo2_HPF.CompProperties_ResourceHarvestable");
                    ConstraintPawnAgeType = AccessTools.TypeByName("Moyo2_HPF.ConstraintPawnAge");

                    if (CompResourceHarvestableType == null)
                    {
                        Log.Warning("[RavenRace] Moyo mod detected but failed to find Moyo2_HPF classes. Compatibility disabled.");
                        IsMoyoActive = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Error initializing Moyo compatibility: {ex}");
                IsMoyoActive = false;
            }
        }

        public static bool HasMoyoBloodline(Pawn pawn)
        {
            if (pawn == null) return false;
            // [Change] Comp_Bloodline -> CompBloodline
            var comp = pawn.TryGetComp<CompBloodline>();
            if (comp != null && comp.BloodlineComposition != null)
            {
                if (comp.BloodlineComposition.ContainsKey("Alien_Moyo"))
                {
                    return comp.BloodlineComposition["Alien_Moyo"] > 0f;
                }
            }
            return false;
        }

        public static void GrantDeepBlueProduction(Pawn pawn)
        {
            if (!IsMoyoActive || pawn == null) return;

            try
            {
                if (pawn.AllComps.Any(c => c.GetType() == CompResourceHarvestableType)) return;

                object constraintObj = Activator.CreateInstance(ConstraintPawnAgeType);
                AccessTools.Field(ConstraintPawnAgeType, "age").SetValue(constraintObj, 7);
                object enumOver = Enum.ToObject(AccessTools.TypeByName("Moyo2_HPF.AgeCompareType"), 3);
                AccessTools.Field(ConstraintPawnAgeType, "compareType").SetValue(constraintObj, enumOver);

                CompProperties props = (CompProperties)Activator.CreateInstance(CompProperties_ResourceHarvestableType);

                AccessTools.Field(CompProperties_ResourceHarvestableType, "harvestJobDef").SetValue(props, HarvestJobDef);
                AccessTools.Field(CompProperties_ResourceHarvestableType, "thingDef").SetValue(props, BlueSerumDef);
                AccessTools.Field(CompProperties_ResourceHarvestableType, "intervalDays").SetValue(props, 8f);
                AccessTools.Field(CompProperties_ResourceHarvestableType, "amount").SetValue(props, 2);
                AccessTools.Field(CompProperties_ResourceHarvestableType, "translationKey").SetValue(props, "Moyo2_HPF_inspectText");
                AccessTools.Field(CompProperties_ResourceHarvestableType, "constraint").SetValue(props, constraintObj);

                ThingComp comp = (ThingComp)Activator.CreateInstance(CompResourceHarvestableType);
                comp.parent = pawn;
                comp.Initialize(props);

                pawn.AllComps.Add(comp);
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Failed to grant Deep Blue production to {pawn.LabelShort}: {ex}");
            }
        }
    }
}