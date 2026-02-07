using System;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Collections.Generic;
using RavenRace.Features.Bloodline;

// [关键] 命名空间统一
namespace RavenRace.Features.Reproduction
{
    public partial class CompSpiritEgg
    {
        public void StartIncubation(List<SoulAltarUpgradeDef> upgrades)
        {
            this.isIncubating = true;
            this.storedUpgradeDefNames.Clear();
            if (upgrades != null)
            {
                foreach (var up in upgrades)
                {
                    if (up != null && !this.storedUpgradeDefNames.Contains(up.defName))
                    {
                        this.storedUpgradeDefNames.Add(up.defName);
                    }
                }
            }
        }

        public void TickIncubation(float multiplier = 1f)
        {
            if (!isIncubating) return;

            float total = TotalTicksNeeded;
            if (total <= 0) total = 60000f;

            float progressPerTick = (1f / total) * multiplier;
            progress += progressPerTick;

            if (progress >= 1f)
            {
                Hatch();
            }
        }

        public void Hatch()
        {
            if (this.parent.Destroyed) return;

            try
            {
                PawnKindDef kindToSpawn = this.pawnKind ?? PawnKindDefOf.Colonist;

                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: kindToSpawn,
                    faction: this.faction ?? Faction.OfPlayer,
                    context: PawnGenerationContext.NonPlayer,
                    allowDowned: true,
                    canGeneratePawnRelations: false,
                    fixedBiologicalAge: 3f,
                    fixedChronologicalAge: 0f,
                    fixedIdeo: null,
                    forcedEndogenes: this.geneSet?.GenesListForReading,
                    forceGenerateNewPawn: true
                );

                Pawn baby = PawnGenerator.GeneratePawn(request);
                if (baby == null)
                {
                    Log.Error("[RavenRace] Failed to generate baby pawn.");
                    return;
                }

                if (baby.apparel != null)
                {
                    baby.apparel.DestroyAll();
                }
                if (baby.equipment != null)
                {
                    baby.equipment.DestroyAllEquipment();
                }

                GenSpawn.Spawn(baby, this.parent.PositionHeld, this.parent.MapHeld, WipeMode.VanishOrMoveAside);

                ApplyBloodlineData(baby);
                ApplyParentRelations(baby);
                ApplyStoredUpgrades(baby);

                if (baby.jobs != null) baby.jobs.StopAll();

                this.parent.Destroy();

                Find.LetterStack.ReceiveLetter(
                    "LetterLabelHatch".Translate(),
                    "LetterTextHatch".Translate(baby.LabelShort),
                    LetterDefOf.PositiveEvent,
                    baby
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Critical error hatching spirit egg: {ex}");
            }
        }

        private void ApplyBloodlineData(Pawn baby)
        {
            CompBloodline bComp = baby.TryGetComp<CompBloodline>();
            if (bComp != null)
            {
                bComp.GoldenCrowConcentration = this.goldenCrowConcentration;
                bComp.SetBloodlineComposition(this.bloodlineComposition);
                bComp.RefreshAbilities();
            }
        }

        private void ApplyParentRelations(Pawn baby)
        {
            Pawn realMother = FindPawnByIdAllWorld(motherId);
            Pawn realFather = FindPawnByIdAllWorld(fatherId);

            if (realMother != null)
            {
                baby.relations.AddDirectRelation(PawnRelationDefOf.Parent, realMother);
            }

            if (realFather != null)
            {
                baby.relations.AddDirectRelation(PawnRelationDefOf.Parent, realFather);
            }
        }

        public Pawn FindPawnByIdAllWorld(string id)
        {
            if (id.NullOrEmpty()) return null;

            foreach (var map in Find.Maps)
            {
                if (map.mapPawns == null) continue;
                foreach (var p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p.ThingID == id) return p;
                }
            }

            if (Find.WorldPawns != null)
            {
                var all = Find.WorldPawns.AllPawnsAliveOrDead;
                if (all != null)
                {
                    for (int i = all.Count - 1; i >= 0; i--)
                    {
                        if (all[i].ThingID == id) return all[i];
                    }
                }
            }

            return null;
        }
    }
}