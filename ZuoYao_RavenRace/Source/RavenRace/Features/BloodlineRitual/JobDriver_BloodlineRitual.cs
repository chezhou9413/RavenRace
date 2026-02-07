using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using RavenRace.Features.Bloodline;
using RavenRace.Features.Reproduction; // [Added]

namespace RavenRace.Features.BloodlineRitual
{
    public class JobDriver_BloodlineRitual : JobDriver
    {
        private const int RitualDuration = 5000;
        private const TargetIndex AltarInd = TargetIndex.A;

        protected Building_Cradle Altar => (Building_Cradle)job.GetTarget(AltarInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Altar, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(AltarInd);
            this.FailOn(() => Altar.GetDirectlyHeldThings().Count == 0);

            yield return Toils_Goto.GotoThing(AltarInd, PathEndMode.InteractionCell);

            Toil ritual = ToilMaker.MakeToil("DoRitual");
            ritual.defaultCompleteMode = ToilCompleteMode.Delay;
            ritual.defaultDuration = RitualDuration;
            ritual.WithProgressBar(AltarInd, () => 1f - (float)ritual.actor.jobs.curDriver.ticksLeftThisToil / RitualDuration);

            ritual.initAction = () =>
            {
                SoundDefOf.PsychicPulseGlobal.PlayOneShot(pawn);
            };

            ritual.tickAction = () =>
            {
                pawn.rotationTracker.FaceTarget(Altar);
                if (pawn.IsHashIntervalTick(100))
                {
                    FleckMaker.ThrowLightningGlow(Altar.TrueCenter(), pawn.Map, 1.5f);
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);
                }
            };

            yield return ritual;

            yield return new Toil
            {
                initAction = () =>
                {
                    CompleteRitual();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private void CompleteRitual()
        {
            if (Altar.GetDirectlyHeldThings().Count == 0) return;

            Thing eggThing = Altar.GetDirectlyHeldThings()[0];
            // [Change] Comp_SpiritEgg -> CompSpiritEgg
            CompSpiritEgg eggComp = eggThing.TryGetComp<CompSpiritEgg>();

            if (eggComp != null)
            {
                AbsorbBloodline(pawn, eggComp);
            }

            eggThing.Destroy(DestroyMode.Vanish);
            FleckMaker.ThrowLightningGlow(pawn.TrueCenter(), pawn.Map, 3.0f);
            Messages.Message("RavenRace_Ritual_AbsorptionSuccess".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);
        }

        private void AbsorbBloodline(Pawn invoker, CompSpiritEgg egg)
        {
            CompBloodline invokerBlood = invoker.TryGetComp<CompBloodline>();
            if (invokerBlood == null) return;

            float gain = egg.goldenCrowConcentration * 0.2f;
            invokerBlood.GoldenCrowConcentration += gain;

            Dictionary<string, float> newComposition = new Dictionary<string, float>();
            HashSet<string> allKeys = new HashSet<string>();

            if (invokerBlood.BloodlineComposition != null)
                foreach (var k in invokerBlood.BloodlineComposition.Keys) allKeys.Add(k);
            if (egg.bloodlineComposition != null)
                foreach (var k in egg.bloodlineComposition.Keys) allKeys.Add(k);

            foreach (string key in allKeys)
            {
                float valInvoker = invokerBlood.BloodlineComposition.ContainsKey(key) ? invokerBlood.BloodlineComposition[key] : 0f;
                float valEgg = egg.bloodlineComposition.ContainsKey(key) ? egg.bloodlineComposition[key] : 0f;
                float finalVal = (valInvoker * 0.8f) + (valEgg * 0.2f);
                if (finalVal > 0f) newComposition[key] = finalVal;
            }

            invokerBlood.SetBloodlineComposition(newComposition);

            if (invokerBlood.BloodlineComposition.ContainsKey("Raven_Race"))
            {
                if (invokerBlood.BloodlineComposition["Raven_Race"] < 0.5f)
                {
                    float raven = 0.5f;
                    float remaining = 0.5f;
                    float otherSum = 0f;
                    foreach (var k in new List<string>(invokerBlood.BloodlineComposition.Keys))
                    {
                        if (k != "Raven_Race") otherSum += invokerBlood.BloodlineComposition[k];
                    }
                    if (otherSum > 0)
                    {
                        foreach (var k in new List<string>(invokerBlood.BloodlineComposition.Keys))
                        {
                            if (k != "Raven_Race")
                                invokerBlood.BloodlineComposition[k] = (invokerBlood.BloodlineComposition[k] / otherSum) * remaining;
                        }
                    }
                    invokerBlood.BloodlineComposition["Raven_Race"] = raven;
                }
            }
            invokerBlood.RefreshAbilities();
        }
    }
}