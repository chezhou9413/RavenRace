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

            Pawn carrier = null;
            Pawn donor = null;

            bool isMechanoidInvolved = pawn.RaceProps.IsMechanoid || partnerPawn.RaceProps.IsMechanoid;
            bool isSameSex = pawn.gender == partnerPawn.gender;

            if (isMechanoidInvolved)
            {
                if (!pawn.RaceProps.IsMechanoid) carrier = pawn;
                else if (!partnerPawn.RaceProps.IsMechanoid) carrier = partnerPawn;
                if (carrier == null) return;
                donor = (carrier == pawn) ? partnerPawn : pawn;
            }
            else if (isSameSex && (RavenRaceMod.Settings.enableSameSexForceLovin || RavenRaceMod.Settings.enableMalePregnancyEgg))
            {
                donor = pawn;
                carrier = partnerPawn;
            }
            else
            {
                carrier = (pawn.gender == Gender.Female) ? pawn : partnerPawn;
                donor = (carrier == pawn) ? partnerPawn : pawn;
            }

            if (carrier == null || donor == null) return;
            if (carrier.gender == Gender.Male && !RavenRaceMod.Settings.enableMalePregnancyEgg) return;

            float chance = RavenRaceMod.Settings.forcedLovinPregnancyRate;
            if (!RavenRaceMod.Settings.ignoreFertilityForPregnancy)
            {
                float carrierFert = carrier.RaceProps.IsMechanoid ? 0f : carrier.GetStatValue(StatDefOf.Fertility);
                float donorFert = donor.RaceProps.IsMechanoid ? 1f : donor.GetStatValue(StatDefOf.Fertility);
                chance *= carrierFert * donorFert;
            }

            if (!Rand.Chance(chance)) return;

            bool isCarrierRaven = carrier.def == RavenDefOf.Raven_Race;
            bool isDonorRaven = donor.def == RavenDefOf.Raven_Race;

            if (isCarrierRaven || isDonorRaven)
            {
                AttemptRavenPregnancy(carrier, donor, isMechanoidInvolved);
            }
            else
            {
                AttemptVanillaPregnancy(carrier, donor);
            }
        }

        private void AttemptRavenPregnancy(Pawn carrier, Pawn donor, bool isMechanoidInvolved)
        {
            GeneSet genes;
            if (isMechanoidInvolved)
            {
                genes = new GeneSet();
                if (carrier.genes != null)
                {
                    foreach (var g in carrier.genes.GenesListForReading)
                    {
                        genes.AddGene(g.def);
                    }
                    genes.SetNameDirect("机械混血");
                }
            }
            else
            {
                // [修正] 直接接收返回的 GeneSet 对象
                genes = PregnancyUtility.GetInheritedGeneSet(donor, carrier, out bool success);
                if (!success) return;
            }

            var hediff = (HediffRavenPregnancy)HediffMaker.MakeHediff(RavenDefOf.Raven_Hediff_RavenPregnancy, carrier);
            hediff.Initialize(donor, genes, RavenRaceMod.Settings.forceRavenDescendant);
            carrier.health.AddHediff(hediff);
            carrier.Drawer?.renderer?.SetAllGraphicsDirty();
            Messages.Message($"{carrier.LabelShortCap} 体内开始孕育一枚渡鸦灵卵。", carrier, MessageTypeDefOf.PositiveEvent);
        }

        private void AttemptVanillaPregnancy(Pawn carrier, Pawn donor)
        {
            if (carrier.gender != Gender.Female) return;

            // [修正] 直接接收返回的 GeneSet 对象
            GeneSet genes = PregnancyUtility.GetInheritedGeneSet(donor, carrier, out bool success);
            if (!success) return;

            var hediff = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, carrier);
            hediff.SetParents(carrier, donor, genes);
            carrier.health.AddHediff(hediff);
            carrier.Drawer?.renderer?.SetAllGraphicsDirty();
            Messages.Message($"{carrier.LabelShortCap} 怀孕了。", carrier, MessageTypeDefOf.PositiveEvent);
        }
    }
}