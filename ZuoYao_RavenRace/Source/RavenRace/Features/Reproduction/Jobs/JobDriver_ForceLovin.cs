using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using RavenRace.Features.Reproduction; // [Added]

namespace RavenRace
{
    public class JobDriver_ForceLovin : JobDriver
    {
        private TargetIndex PartnerInd = TargetIndex.A;
        private const int LovinDuration = 2000;
        private const int TicksBetweenHeartMotes = 100;

        private Pawn Partner => (Pawn)job.GetTarget(PartnerInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Partner, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(PartnerInd);
            this.FailOn(() => Partner.Dead);

            Toil gotoToil = Toils_Goto.GotoThing(PartnerInd, PathEndMode.Touch);
            gotoToil.tickAction = delegate
            {
                Pawn target = Partner;
                if (target != null && target.Spawned && !target.Dead && pawn.Position.DistanceTo(target.Position) < 10f)
                {
                    if (target.pather != null && target.pather.Moving && target.CurJobDef != JobDefOf.Wait_MaintainPosture)
                    {
                        Job waitJob = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
                        waitJob.expiryInterval = 120;
                        target.jobs.StartJob(waitJob, JobCondition.InterruptForced);
                        MoteMaker.ThrowText(target.DrawPos, target.Map, "!", 2f);
                    }
                }
            };
            yield return gotoToil;

            Toil prepare = ToilMaker.MakeToil("Prepare");
            prepare.initAction = delegate
            {
                pawn.rotationTracker.FaceCell(Partner.Position);
                if (!Partner.Downed && Partner.health.capacities.CanBeAwake)
                {
                    Partner.rotationTracker.FaceCell(pawn.Position);
                }
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, pawn.Named(HistoryEventArgsNames.Doer)), true);
            };
            prepare.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return prepare;

            Toil lovinToil = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = LovinDuration,
                socialMode = RandomSocialMode.Off
            };

            lovinToil.tickAction = delegate
            {
                pawn.rotationTracker.FaceCell(Partner.Position);
                Partner.rotationTracker.FaceCell(pawn.Position);

                if (pawn.IsHashIntervalTick(TicksBetweenHeartMotes))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart, 0.42f);
                    FleckMaker.ThrowMetaIcon(Partner.Position, Partner.Map, FleckDefOf.Heart, 0.42f);
                }

                if (Partner.Spawned && !Partner.Downed && !Partner.HostileTo(pawn) && Partner.CurJobDef != JobDefOf.Wait_MaintainPosture)
                {
                    Partner.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture), JobCondition.InterruptForced);
                }
            };

            lovinToil.AddFinishAction(delegate
            {
                Pawn partnerPawn = Partner;
                if (partnerPawn == null) return;

                if (partnerPawn.RaceProps.Humanlike)
                {
                    pawn.needs.mood?.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_ForceLovin_Initiator, partnerPawn);
                    partnerPawn.needs.mood?.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_ForceLovin_Recipient, pawn);
                    pawn.interactions.TryInteractWith(partnerPawn, InteractionDefOf.Chitchat);
                    if (partnerPawn.IsPrisonerOfColony)
                    {
                        HandlePrisonerInteraction(partnerPawn);
                    }
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, pawn.Named(HistoryEventArgsNames.Doer)), true);
                }
                else
                {
                    Messages.Message($"{pawn.LabelShort} 完成了与 {partnerPawn.LabelShort} 的跨物种交流。", pawn, MessageTypeDefOf.NeutralEvent);
                }

                AttemptPregnancy(partnerPawn);

                if (!partnerPawn.Downed && !partnerPawn.HostileTo(pawn) && partnerPawn.CurJobDef == JobDefOf.Wait_MaintainPosture)
                {
                    partnerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            });

            yield return lovinToil;
        }

        private void HandlePrisonerInteraction(Pawn prisoner)
        {
            var s = RavenRaceMod.Settings;
            if (s == null) return;
            bool graphicsDirty = false;

            if (prisoner.guest.ExclusiveInteractionMode == PrisonerInteractionModeDefOf.AttemptRecruit && prisoner.guest.resistance > 0)
            {
                float reduction = s.forceLovinResistanceReduction;
                prisoner.guest.resistance = Mathf.Max(0, prisoner.guest.resistance - reduction);
                Messages.Message("RavenRace_Msg_ResistanceLowered".Translate(prisoner.LabelShort, reduction), prisoner, MessageTypeDefOf.PositiveEvent);

                if (prisoner.guest.resistance <= 0 && Rand.Chance(s.forceLovinInstantRecruitChance))
                {
                    InteractionWorker_RecruitAttempt.DoRecruit(pawn, prisoner);
                    AddDominatedRelation(prisoner, pawn);
                    Messages.Message("RavenRace_Msg_InstantRecruit".Translate(prisoner.LabelShort), prisoner, MessageTypeDefOf.PositiveEvent);
                    graphicsDirty = true;
                    if (graphicsDirty) prisoner.Drawer?.renderer?.SetAllGraphicsDirty();
                    return;
                }
            }

            if (prisoner.guest.ExclusiveInteractionMode == PrisonerInteractionModeDefOf.Enslave && prisoner.guest.will > 0)
            {
                float reduction = s.forceLovinWillReduction;
                prisoner.guest.will = Mathf.Max(0, prisoner.guest.will - reduction);
                Messages.Message("RavenRace_Msg_WillLowered".Translate(prisoner.LabelShort, reduction), prisoner, MessageTypeDefOf.PositiveEvent);

                if (prisoner.guest.will <= 0 && Rand.Chance(s.forceLovinInstantRecruitChance))
                {
                    GenGuest.EnslavePrisoner(pawn, prisoner);
                    AddDominatedRelation(prisoner, pawn);
                    Messages.Message("RavenRace_Msg_InstantEnslave".Translate(prisoner.LabelShort), prisoner, MessageTypeDefOf.PositiveEvent);
                    graphicsDirty = true;
                    if (graphicsDirty) prisoner.Drawer?.renderer?.SetAllGraphicsDirty();
                    return;
                }
            }

            if (ModsConfig.IdeologyActive && prisoner.guest.ExclusiveInteractionMode == PrisonerInteractionModeDefOf.Convert && prisoner.Ideo != pawn.Ideo)
            {
                prisoner.ideo.OffsetCertainty(-s.forceLovinCertaintyReduction);
            }

            if (!prisoner.guest.Recruitable && Rand.Chance(s.forceLovinBreakLoyaltyChance))
            {
                prisoner.guest.Recruitable = true;
                prisoner.guest.SetExclusiveInteraction(PrisonerInteractionModeDefOf.AttemptRecruit);
                Messages.Message("RavenRace_Msg_LoyaltyBroken".Translate(prisoner.LabelShort), prisoner, MessageTypeDefOf.PositiveEvent);
                graphicsDirty = true;
            }

            if (graphicsDirty)
            {
                prisoner.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        private void AddDominatedRelation(Pawn subject, Pawn master)
        {
            if (subject != null && master != null)
            {
                subject.relations.AddDirectRelation(RavenDefOf.Raven_Relation_Dominated, master);
            }
        }

        private void AttemptPregnancy(Pawn partnerPawn)
        {
            if (!RavenRaceMod.Settings.enableForceLovinPregnancy || !ModsConfig.BiotechActive) return;

            Pawn male = (pawn.gender == Gender.Male) ? pawn : (partnerPawn.gender == Gender.Male ? partnerPawn : null);
            Pawn female = (pawn.gender == Gender.Female) ? pawn : (partnerPawn.gender == Gender.Female ? partnerPawn : null);
            Pawn carrier = female;
            Pawn donor = male;

            if (RavenRaceMod.Settings.enableMalePregnancyEgg && pawn.gender == partnerPawn.gender && pawn.gender == Gender.Male)
            {
                bool isPawnRaven = pawn.def == RavenDefOf.Raven_Race;
                bool isPartnerRaven = partnerPawn.def == RavenDefOf.Raven_Race;
                if (isPawnRaven || isPartnerRaven)
                {
                    carrier = isPawnRaven ? pawn : partnerPawn;
                    donor = (carrier == pawn) ? partnerPawn : pawn;
                }
            }

            if (carrier == null || donor == null) return;

            float chance = RavenRaceMod.Settings.forcedLovinPregnancyRate;
            if (!RavenRaceMod.Settings.ignoreFertilityForPregnancy)
            {
                chance *= carrier.GetStatValue(StatDefOf.Fertility) * donor.GetStatValue(StatDefOf.Fertility);
            }

            if (Rand.Chance(chance))
            {
                GeneSet genes = PregnancyUtility.GetInheritedGeneSet(donor, carrier, out bool success);
                if (success)
                {
                    // [Change] Hediff_RavenPregnancy -> HediffRavenPregnancy
                    HediffRavenPregnancy hediff = (HediffRavenPregnancy)HediffMaker.MakeHediff(HediffDef.Named("Raven_Hediff_RavenPregnancy"), carrier);
                    hediff.Initialize(donor, genes, RavenRaceMod.Settings.forceRavenDescendant);
                    carrier.health.AddHediff(hediff);
                    carrier.Drawer?.renderer?.SetAllGraphicsDirty();
                }
            }
        }
    }
}