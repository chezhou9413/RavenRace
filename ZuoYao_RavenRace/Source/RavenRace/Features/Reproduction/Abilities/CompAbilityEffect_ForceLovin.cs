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
            Thing targetThing = target.Thing;
            if (targetThing == null) return false;

            // =====================================
            // 1. 处理选择建筑 (墙) 的彩蛋情况
            // =====================================
            if (targetThing is Building b)
            {
                if (!RavenRaceMod.Settings.enableBuildingLovin)
                {
                    if (throwMessages) Messages.Message("在设置中未开启“与建筑交配”的彩蛋功能。", targetThing, MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // 严格使用反编译确定的 BuildingProperties.isWall 属性来判定是否为墙体
                if (b.def.building == null || !b.def.building.isWall)
                {
                    if (throwMessages) Messages.Message("你只能对着一面结实的墙发情。", targetThing, MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                if (!this.parent.pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
                {
                    if (throwMessages) Messages.Message("CannotReach".Translate(), targetThing, MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                return true;
            }

            // =====================================
            // 2. 处理常规生物和机械体的情况
            // =====================================
            Pawn targetPawn = targetThing as Pawn;
            if (targetPawn == null) return false;

            // 检查敌人状态：如果敌对且未倒地，禁止选中
            if (targetPawn.HostileTo(parent.pawn) && !targetPawn.Downed)
            {
                if (throwMessages) Messages.Message("Invalid Target: Enemy must be downed.", targetPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // 种族判定 (Humanlike vs Animal/Mech)
            bool isMuffalo = targetPawn.def.defName == "Muffalo";
            bool isMech = targetPawn.RaceProps.IsMechanoid;

            if (targetPawn.RaceProps.Animal)
            {
                if (!isMuffalo || !RavenRaceMod.Settings.enableMuffaloPrank)
                {
                    if (throwMessages) Messages.Message("Invalid Target: Must be Humanlike.", targetPawn, MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            else if (isMech)
            {
                if (!RavenRaceMod.Settings.enableMechanoidLovin)
                {
                    if (throwMessages) Messages.Message("Target is a mechanoid (Feature disabled in settings).", targetPawn, MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }
            else if (!targetPawn.RaceProps.Humanlike)
            {
                // 既不是动物、不是机械、也不是人 (可能是奇怪的Mod种族)
                return false;
            }

            // 性别判定
            // 如果开启了同性/男性生蛋/机械族(机械族通常无性别)，则跳过性别检查
            bool allowSameSex = RavenRaceMod.Settings.enableMalePregnancyEgg ||
                                RavenRaceMod.Settings.enableSameSexForceLovin ||
                                RavenRaceMod.Settings.enableMechanoidLovin;

            if (!allowSameSex && targetPawn.gender == this.parent.pawn.gender)
            {
                if (throwMessages) Messages.Message("Must target opposite gender.", targetPawn, MessageTypeDefOf.RejectInput, false);
                return false;
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
            Thing targetThing = target.Thing;

            if (pawn != null && targetThing != null)
            {
                // 创建 Job 时，传入通用的 Thing（Building 或 Pawn 都可以被接受为 TargetA）
                Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_ForceLovin, targetThing);
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