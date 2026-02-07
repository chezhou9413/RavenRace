using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RavenRace.Features.Reproduction.Harmony
{
    [HarmonyPatch(typeof(PregnancyUtility), "ApplyBirthOutcome")]
    public static class Patch_ApplyBirthOutcome
    {
        [HarmonyPrefix]
        public static bool Prefix(
            RitualOutcomePossibility outcome,
            float quality,
            Precept_Ritual ritual,
            List<GeneDef> genes,
            Pawn geneticMother,
            Thing birtherThing,
            Pawn father,
            Pawn doctor,
            LordJob_Ritual lordJobRitual,
            RitualRoleAssignments assignments,
            bool preventLetter,
            ref Thing __result)
        {
            Pawn mother = birtherThing as Pawn;
            if (mother == null) return true;

            bool shouldLayEgg = false;
            bool isMalePregnancy = false;

            bool enableMaleEgg = RavenRaceMod.Settings.enableMalePregnancyEgg;

            if (enableMaleEgg && mother.gender == Gender.Male)
            {
                if (mother.def == RavenDefOf.Raven_Race || (father != null && father.def == RavenDefOf.Raven_Race))
                {
                    shouldLayEgg = true;
                    isMalePregnancy = true;
                }
            }
            else if (mother.def == RavenDefOf.Raven_Race)
            {
                shouldLayEgg = true;
            }
            else if (RavenRaceMod.Settings.ravenFatherDeterminesEgg && father != null && father.def == RavenDefOf.Raven_Race)
            {
                shouldLayEgg = true;
            }

            if (!shouldLayEgg) return true;

            bool success;
            List<GeneDef> inheritedGenesList;
            try
            {
                Pawn p1 = father;
                Pawn p2 = geneticMother ?? mother;

                if (p1 != null && p2 != null && p1.gender == p2.gender)
                {
                    inheritedGenesList = new List<GeneDef>();
                }
                else
                {
                    inheritedGenesList = PregnancyUtility.GetInheritedGenes(p1, p2, out success);
                }
            }
            catch
            {
                inheritedGenesList = new List<GeneDef>();
            }

            GeneSet inheritedGeneSet = new GeneSet();
            foreach (var g in inheritedGenesList) inheritedGeneSet.AddGene(g);
            if (father != null && geneticMother != null && GeneUtility.SameHeritableXenotype(father, geneticMother))
            {
                inheritedGeneSet.SetNameDirect(father.genes?.xenotypeName);
            }

            Thing eggThing = ThingMaker.MakeThing(RavenDefOf.Raven_SpiritEgg);

            // [Change] Comp_SpiritEgg -> CompSpiritEgg
            CompSpiritEgg eggComp = eggThing.TryGetComp<CompSpiritEgg>();
            if (eggComp != null)
            {
                eggComp.Initialize(mother, father, inheritedGeneSet);
            }

            GenSpawn.Spawn(eggThing, mother.Position, mother.Map);

            mother.health.AddHediff(HediffDefOf.PostpartumExhaustion);

            if (isMalePregnancy)
            {
                HediffDef painDef = DefDatabase<HediffDef>.GetNamedSilentFail("PainShock");
                mother.health.AddHediff(painDef ?? HediffDefOf.Anesthetic);
            }
            else
            {
                mother.health.AddHediff(HediffDefOf.Lactating);
                FilthMaker.TryMakeFilth(mother.Position, mother.Map, ThingDefOf.Filth_AmnioticFluid, 3);
            }

            RemovePregnancyHediffs(mother);

            if (!preventLetter)
            {
                string label = "RavenRace_LetterLabel_EggLaid".Translate();
                string text = "RavenRace_LetterText_EggLaid".Translate(mother.LabelShort, father != null ? father.LabelShort : "Unknown");
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, eggThing);
            }

            var featherComp = mother.TryGetComp<CompRavenFeatherDrop>();
            featherComp?.Notify_Birth();

            __result = eggThing;
            return false;
        }

        private static void RemovePregnancyHediffs(Pawn pawn)
        {
            try
            {
                var hediffsToRemove = new List<HediffDef> {
                    HediffDefOf.PregnantHuman,
                    HediffDefOf.PregnancyLabor,
                    HediffDefOf.PregnancyLaborPushing
                };

                for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff h = pawn.health.hediffSet.hediffs[i];
                    if (hediffsToRemove.Contains(h.def))
                    {
                        pawn.health.RemoveHediff(h);
                    }
                }
            }
            catch (Exception ex)
            {
                RavenModUtility.LogDebug($"Error removing pregnancy hediffs: {ex.Message}");
            }
        }
    }
}