using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace RavenRace.Features.MechanicalAngel
{
    public class CompProperties_AegisCore : CompProperties
    {
        public CompProperties_AegisCore()
        {
            this.compClass = typeof(CompAegisCore);
        }
    }

    /// <summary>
    /// 艾吉斯核心组件：管理主动发情机制、强制暴走状态，以及赋予她独有的“拾取武器”能力。
    /// </summary>
    public class CompAegisCore : ThingComp
    {
        public bool allowLustCharge = true;
        public bool isRampaging = false;

        public Pawn Pawn => this.parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref allowLustCharge, "allowLustCharge", true);
            Scribe_Values.Look(ref isRampaging, "isRampaging", false);
        }

        public override void CompTick()
        {
            base.CompTick();
            // 【性能优化】从 CompTick (每帧) 改为 CompTickRare (每 250 ticks)，大幅降低性能开销
        }

        public override void CompTickRare()
        {
            base.CompTickRare();

            var lustNeed = Pawn.needs.TryGetNeed<Need_AegisLust>();
            if (lustNeed != null)
            {
                if (lustNeed.CurLevelPercentage < 0.20f && !isRampaging)
                {
                    TriggerLustRampage();
                }
                else if (lustNeed.CurLevelPercentage >= 0.50f && isRampaging)
                {
                    EndLustRampage();
                }
            }
        }

        private void TriggerLustRampage()
        {
            isRampaging = true;
            var rampageHediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("Raven_Hediff_AegisRampage"), Pawn);
            Pawn.health.AddHediff(rampageHediff);

            if (Pawn.drafter != null) Pawn.drafter.Drafted = false;
            Pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, true);

            Messages.Message("艾吉斯的淫能核心濒临枯竭，底层协议崩溃，进入了无法控制的发情暴走状态！", Pawn, MessageTypeDefOf.ThreatBig);
        }

        private void EndLustRampage()
        {
            isRampaging = false;
            var rampageHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("Raven_Hediff_AegisRampage"));
            if (rampageHediff != null)
            {
                Pawn.health.RemoveHediff(rampageHediff);
            }
            Messages.Message($"艾吉斯的淫能已得到滋润，退出了发情暴走状态。", Pawn, MessageTypeDefOf.PositiveEvent);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Pawn.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "允许主动榨取",
                    defaultDesc = "开启后，当艾吉斯的淫能过低时，她会自动寻找主人进行榨取。关闭后她只会寻找最近的殖民者进行暴走榨取。",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/HeartIcon", true),
                    isActive = () => allowLustCharge,
                    toggleAction = () => allowLustCharge = !allowLustCharge
                };
            }

            if (Pawn.Faction == Faction.OfPlayer && !Pawn.Downed)
            {
                yield return new Command_Action
                {
                    defaultLabel = "更换武器...",
                    defaultDesc = "命令艾吉斯拾取地上的武器或与他人交换武器。这是为她更换装备的唯一方式。",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                    action = delegate
                    {
                        var targetingParams = new TargetingParameters
                        {
                            canTargetItems = true,
                            canTargetPawns = true,
                            mapObjectTargetsMustBeAutoAttackable = false,
                            validator = (target) =>
                            {
                                if (target.Thing is Pawn p && p.equipment?.Primary != null) return true;
                                if (target.Thing != null && target.Thing.def.IsWeapon) return true;
                                return false;
                            }
                        };
                        Find.Targeter.BeginTargeting(targetingParams, (target) =>
                        {
                            // 【核心修复】创建更健壮的换武器 Job
                            var equipJob = JobMaker.MakeJob(JobDefOf.Equip, target);
                            Pawn.jobs.TryTakeOrderedJob(equipJob, JobTag.Misc);
                        });
                    }
                };
            }
        }
    }
}