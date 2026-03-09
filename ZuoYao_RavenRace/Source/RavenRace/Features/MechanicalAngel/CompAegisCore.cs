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

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (Pawn == null || Pawn.Dead || Pawn.needs == null) return;

            // 【核心修复】直接使用原版的 energy (已经被我们伪装成了淫能)
            var lustNeed = Pawn.needs.energy;
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
            if (RavenDefOf.Raven_Hediff_AegisRampage != null)
            {
                var rampageHediff = HediffMaker.MakeHediff(RavenDefOf.Raven_Hediff_AegisRampage, Pawn);
                Pawn.health.AddHediff(rampageHediff);
            }

            if (Pawn.drafter != null) Pawn.drafter.Drafted = false;
            Pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, true);

            Messages.Message("艾吉斯的淫能核心濒临枯竭，底层协议崩溃，进入了无法控制的发情暴走状态！", Pawn, MessageTypeDefOf.ThreatBig);
        }

        private void EndLustRampage()
        {
            isRampaging = false;
            if (RavenDefOf.Raven_Hediff_AegisRampage != null)
            {
                var rampageHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(RavenDefOf.Raven_Hediff_AegisRampage);
                if (rampageHediff != null) Pawn.health.RemoveHediff(rampageHediff);
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
                    defaultDesc = "命令艾吉斯拾取地上的武器或剥夺他人的武器。",
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
                            Thing targetThing = target.Thing;

                            if (targetThing is Pawn targetPawn)
                            {
                                ThingWithComps targetWeapon = targetPawn.equipment?.Primary;
                                if (targetWeapon != null)
                                {
                                    ThingWithComps droppedWeapon;
                                    targetPawn.equipment.TryDropEquipment(targetWeapon, out droppedWeapon, targetPawn.Position, false);
                                    if (droppedWeapon != null) targetThing = droppedWeapon;
                                }
                            }

                            if (targetThing != null && targetThing.Spawned && targetThing.def.IsWeapon)
                            {
                                var equipJob = JobMaker.MakeJob(JobDefOf.Equip, targetThing);
                                Pawn.jobs.TryTakeOrderedJob(equipJob, JobTag.Misc);
                            }
                        });
                    }
                };
            }
        }
    }
}