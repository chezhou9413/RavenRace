using System;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace
{
    public class CompProperties_AbilityForceLovin : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityForceLovin()
        {
            this.compClass = typeof(CompAbilityEffect_ForceLovin);
        }
    }

    public class CompAbilityEffect_ForceLovin : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn targetPawn = target.Pawn;
            if (targetPawn == null) return false;

            // [新增] 检查敌人状态：如果敌对且未倒地，禁止选中
            if (targetPawn.HostileTo(parent.pawn) && !targetPawn.Downed)
            {
                if (throwMessages) Messages.Message("Invalid Target: Enemy must be downed.", targetPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // 雪牛彩蛋判定
            bool isMuffalo = targetPawn.def.defName == "Muffalo";
            if (targetPawn.RaceProps.Animal)
            {
                if (!isMuffalo || !RavenRaceMod.Settings.enableMuffaloPrank)
                {
                    if (throwMessages) Messages.Message("Invalid Target: Must be Humanlike.", targetPawn, MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            else
            {
                bool allowSameSex = RavenRaceMod.Settings.enableMalePregnancyEgg || RavenRaceMod.Settings.enableSameSexForceLovin;
                if (!allowSameSex && targetPawn.gender == this.parent.pawn.gender)
                {
                    if (throwMessages) Messages.Message("Must target opposite gender.", targetPawn, MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }

            if (!this.parent.pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                if (throwMessages) Messages.Message("Cannot reach target.", targetPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = this.parent.pawn;
            Pawn targetPawn = target.Pawn;

            if (pawn != null && targetPawn != null)
            {
                Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_ForceLovin, targetPawn);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                float days = RavenRaceMod.Settings.forceLovinCooldownDays;
                if (days > 0)
                {
                    int ticks = (int)(days * 60000f);
                    this.parent.StartCooldown(ticks);
                }
            }
        }
    }
}