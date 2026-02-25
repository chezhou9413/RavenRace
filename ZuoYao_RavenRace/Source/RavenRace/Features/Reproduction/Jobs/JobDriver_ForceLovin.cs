using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using RavenRace.Features.Reproduction;

namespace RavenRace
{
    public class JobDriver_ForceLovin : JobDriver
    {
        private TargetIndex PartnerInd = TargetIndex.A;
        private const int LovinDuration = 2000;
        private const int TicksBetweenHeartMotes = 100;

        // 【核心修改】将目标泛化为 Thing，并提供一个尝试转换为 Pawn 的安全访问器
        private Thing PartnerThing => job.GetTarget(PartnerInd).Thing;
        private Pawn PartnerPawn => PartnerThing as Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(PartnerThing, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(PartnerInd);
            // 只有当目标是生物时才检查是否死亡（建筑没有 Dead 属性）
            this.FailOn(() => PartnerPawn != null && PartnerPawn.Dead);
            this.FailOn(() => PartnerThing is Building b && b.Destroyed);

            Toil gotoToil = Toils_Goto.GotoThing(PartnerInd, PathEndMode.Touch);
            gotoToil.tickAction = delegate
            {
                if (PartnerPawn != null && PartnerPawn.Spawned && !PartnerPawn.Dead && pawn.Position.DistanceTo(PartnerPawn.Position) < 10f)
                {
                    if (PartnerPawn.pather != null && PartnerPawn.pather.Moving && PartnerPawn.CurJobDef != JobDefOf.Wait_MaintainPosture)
                    {
                        Job waitJob = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
                        waitJob.expiryInterval = 120;
                        PartnerPawn.jobs.StartJob(waitJob, JobCondition.InterruptForced);
                        MoteMaker.ThrowText(PartnerPawn.DrawPos, PartnerPawn.Map, "!", 2f);
                    }
                }
            };
            yield return gotoToil;

            Toil prepare = ToilMaker.MakeToil("Prepare");
            prepare.initAction = delegate
            {
                pawn.rotationTracker.FaceCell(PartnerThing.Position);
                if (PartnerPawn != null && !PartnerPawn.Downed && PartnerPawn.health.capacities.CanBeAwake)
                {
                    PartnerPawn.rotationTracker.FaceCell(pawn.Position);
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
                pawn.rotationTracker.FaceCell(PartnerThing.Position);
                if (PartnerPawn != null)
                {
                    PartnerPawn.rotationTracker.FaceCell(pawn.Position);
                }

                if (pawn.IsHashIntervalTick(TicksBetweenHeartMotes))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart, 0.42f);
                    if (PartnerPawn != null)
                    {
                        FleckMaker.ThrowMetaIcon(PartnerPawn.Position, PartnerPawn.Map, FleckDefOf.Heart, 0.42f);
                    }
                    else
                    {
                        // 对着墙发情时，墙也会冒爱心！
                        FleckMaker.ThrowMetaIcon(PartnerThing.Position, PartnerThing.Map, FleckDefOf.Heart, 0.42f);
                    }
                }

                if (PartnerPawn != null && PartnerPawn.Spawned && !PartnerPawn.Downed && !PartnerPawn.HostileTo(pawn) && PartnerPawn.CurJobDef != JobDefOf.Wait_MaintainPosture)
                {
                    PartnerPawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture), JobCondition.InterruptForced);
                }
            };

            lovinToil.AddFinishAction(delegate
            {
                if (PartnerPawn != null)
                {
                    // === 和生物交配的结算逻辑 ===
                    if (PartnerPawn.RaceProps.Humanlike)
                    {
                        pawn.needs.mood?.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_ForceLovin_Initiator, PartnerPawn);
                        PartnerPawn.needs.mood?.thoughts.memories.TryGainMemory(RavenDefOf.Raven_Thought_ForceLovin_Recipient, pawn);
                        pawn.interactions.TryInteractWith(PartnerPawn, InteractionDefOf.Chitchat);
                        if (PartnerPawn.IsPrisonerOfColony)
                        {
                            HandlePrisonerInteraction(PartnerPawn);
                        }
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, pawn.Named(HistoryEventArgsNames.Doer)), true);
                    }
                    else
                    {
                        Messages.Message($"{pawn.LabelShort} 完成了与 {PartnerPawn.LabelShort} 的跨物种交流。", pawn, MessageTypeDefOf.NeutralEvent);
                    }

                    AttemptPregnancy(PartnerPawn);

                    if (!PartnerPawn.Downed && !PartnerPawn.HostileTo(pawn) && PartnerPawn.CurJobDef == JobDefOf.Wait_MaintainPosture)
                    {
                        PartnerPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                }
                else if (PartnerThing is Building b)
                {
                    // === 和建筑(墙)交配的彩蛋结算逻辑 ===
                    Messages.Message($"{pawn.LabelShort} 满意地拍了拍那面 {b.LabelShort}。似乎连砖缝里都充满了泥泞的痕迹。", pawn, MessageTypeDefOf.NeutralEvent);
                    AttemptBuildingPregnancy(b);
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

        // ==============================================================
        // 处理与建筑(墙)的怀孕逻辑
        // ==============================================================
        private void AttemptBuildingPregnancy(Building building)
        {
            if (!RavenRaceMod.Settings.enableForceLovinPregnancy || !ModsConfig.BiotechActive) return;

            // 如果渡鸦是男性且未开启男性生蛋，跳过
            if (pawn.gender == Gender.Male && !RavenRaceMod.Settings.enableMalePregnancyEgg) return;

            float chance = RavenRaceMod.Settings.forcedLovinPregnancyRate;
            if (!RavenRaceMod.Settings.ignoreFertilityForPregnancy)
            {
                chance *= pawn.GetStatValue(StatDefOf.Fertility);
            }

            if (!Rand.Chance(chance)) return;

            // 由于父亲是墙，没有基因可言，我们手动构造一个假的单亲基因集以防原版崩溃
            GeneSet genes = new GeneSet();
            if (pawn.genes != null)
            {
                foreach (var g in pawn.genes.GenesListForReading) genes.AddGene(g.def);
                genes.SetNameDirect("坚不可摧"); // 给个幽默的异种族名称
            }

            // 初始化怀孕 Hediff，并传入自定义血脉标识 "Wall"
            var hediff = (HediffRavenPregnancy)HediffMaker.MakeHediff(RavenDefOf.Raven_Hediff_RavenPregnancy, pawn);
            hediff.Initialize(null, genes, RavenRaceMod.Settings.forceRavenDescendant, "Wall");
            pawn.health.AddHediff(hediff);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();

            Messages.Message($"{pawn.LabelShortCap} 与 {building.LabelShort} 深入交流后，体内竟然奇迹般地开始孕育一枚渡鸦灵卵。", pawn, MessageTypeDefOf.PositiveEvent);
        }

        // ==============================================================
        // 原有与生物的怀孕逻辑
        // ==============================================================
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
                genes = PregnancyUtility.GetInheritedGeneSet(donor, carrier, out bool success);
                if (!success) return;
            }

            var hediff = (HediffRavenPregnancy)HediffMaker.MakeHediff(RavenDefOf.Raven_Hediff_RavenPregnancy, carrier);
            // 这里传入 null 作为 customBloodline 参数，走原有的生物遗传路线
            hediff.Initialize(donor, genes, RavenRaceMod.Settings.forceRavenDescendant, null);
            carrier.health.AddHediff(hediff);
            carrier.Drawer?.renderer?.SetAllGraphicsDirty();
            Messages.Message($"{carrier.LabelShortCap} 体内开始孕育一枚渡鸦灵卵。", carrier, MessageTypeDefOf.PositiveEvent);
        }

        private void AttemptVanillaPregnancy(Pawn carrier, Pawn donor)
        {
            if (carrier.gender != Gender.Female) return;

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