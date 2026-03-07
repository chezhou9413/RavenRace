using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;

namespace RavenRace.Features.Servitude
{
    /// <summary>
    /// 侍奉系统的核心 AI 驱动节点。
    /// 负责为侍奉者寻找互动机会或主动陪伴主人。
    /// </summary>
    public class JobGiver_Servitude : ThinkNode_JobGiver
    {
        private const float MaxFollowDistance = 15f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            var manager = ServitudeManager.Get();
            if (manager == null || !manager.IsServant(pawn)) return null;

            Pawn master = manager.GetMaster(pawn);
            // 确保主人在同一地图且存活、未处于倒地状态
            if (master == null || master.Map != pawn.Map || master.Dead || master.Downed) return null;

            // 1. 尝试触发特殊的侍奉互动 (勾引、喂食等)
            var interactionJob = TryGiveInteractionJob(pawn, master, manager);
            if (interactionJob != null) return interactionJob;

            // 2. 主动陪伴逻辑：如果侍奉者当前闲置，且离主人过远，则移动到主人身边
            if (pawn.mindState.IsIdle)
            {
                if (!pawn.Position.InHorDistOf(master.Position, MaxFollowDistance))
                {
                    // 分配跟随任务
                    return JobMaker.MakeJob(RavenDefOf.Raven_Job_FollowMaster, master);
                }
            }

            return null;
        }

        private Job TryGiveInteractionJob(Pawn servant, Pawn master, ServitudeManager manager)
        {
            // 获取配置 Def
            var configDef = DefDatabase<ServitudeConfigDef>.GetNamed("Raven_ServitudeBond", false);
            if (configDef == null) return null;

            var extension = configDef.GetModExtension<ServitudeDefExtension>();
            if (extension == null || extension.interactions.NullOrEmpty()) return null;

            // 随机顺序检查互动，增加多样性
            var potentialInteractions = extension.interactions.InRandomOrder().ToList();

            foreach (var interaction in potentialInteractions)
            {
                // 基于全局设置缩放触发概率
                float chance = interaction.baseChance * RavenRaceMod.Settings.servitudeInteractionChance / 0.1f;

                // 冷却检查与概率判定
                if (manager.IsOnCooldown(servant, interaction.jobDef) || !Rand.Chance(chance))
                    continue;

                // 特定场合检查 (如：擦拭身体仅在洗澡时)
                if (interaction.requiredMasterJobDef != null)
                {
                    if (master.CurJob == null || master.CurJob.def != interaction.requiredMasterJobDef)
                    {
                        continue;
                    }
                }

                bool canDo = false;
                // 检查是否为技能驱动型互动
                var abilityDef = DefDatabase<AbilityDef>.AllDefs.FirstOrDefault(a => a.jobDef == interaction.jobDef);

                if (abilityDef != null)
                {
                    var ability = servant.abilities?.GetAbility(abilityDef);
                    if (ability != null && ability.CanCast && ability.CanApplyOn(new LocalTargetInfo(master)))
                    {
                        canDo = true;
                    }
                }
                else
                {
                    // 非技能型互动逻辑检查 (如：喂食仅在主人饥饿时)
                    if (interaction.jobDef == RavenDefOf.Raven_Job_FeedMaster)
                    {
                        if (master.needs?.food != null && master.needs.food.CurLevelPercentage < master.needs.food.PercentageThreshHungry)
                        {
                            canDo = true;
                        }
                    }
                    else
                    {
                        // 基础可达性检查
                        canDo = servant.CanReach(master, PathEndMode.Touch, Danger.None);
                    }
                }

                if (canDo)
                {
                    // 设置冷却时间，应用全局倍率
                    var tempInteraction = new ServitudeInteraction
                    {
                        jobDef = interaction.jobDef,
                        cooldownTicks = (int)(interaction.cooldownTicks * RavenRaceMod.Settings.servitudeCooldownMultiplier)
                    };
                    manager.StartCooldown(servant, tempInteraction);

                    // 视觉特效反馈
                    if (interaction.fleckDef != null)
                    {
                        FleckMaker.ThrowMetaIcon(servant.Position, servant.Map, interaction.fleckDef);
                        FleckMaker.ThrowMetaIcon(master.Position, master.Map, interaction.fleckDef);
                    }

                    // 文字提示反馈
                    if (!string.IsNullOrEmpty(interaction.letterText))
                    {
                        Find.LetterStack.ReceiveLetter(
                            interaction.letterLabel,
                            interaction.letterText.Formatted(servant.Named("SERVANT"), master.Named("MASTER")),
                            LetterDefOf.NeutralEvent,
                            new LookTargets(servant, master));
                    }

                    // 【核心RJW兼容修改】
                    // 如果侍奉互动是“强制求爱”，并且RJW已激活，则将Job替换为RJW的性爱Job。
                    if (interaction.jobDef == RavenDefOf.Raven_Job_ForceLovin && ModsConfig.IsActive("rim.job.world"))
                    {
                        // [核心修复] RJW 对应的通用自愿站立交配 Job 叫做 "Quickie"，而不是 "rjw_fuck"
                        JobDef rjwJobDef = DefDatabase<JobDef>.GetNamed("Quickie", false);
                        if (rjwJobDef != null)
                        {
                            Log.Message($"[RavenRace Servitude] RJW is active. Redirecting seduction from {servant.LabelShort} to {master.LabelShort} to use 'Quickie' job.");
                            return JobMaker.MakeJob(rjwJobDef, master);
                        }
                        else
                        {
                            Log.Warning("[RavenRace Servitude] RJW is active, but 'Quickie' JobDef was not found. Falling back to vanilla ForceLovin job.");
                        }
                    }

                    // 特殊处理喂食寻找食物
                    if (interaction.jobDef == RavenDefOf.Raven_Job_FeedMaster)
                    {
                        Thing food = null;
                        ThingDef foodDef = null;
                        if (FoodUtility.TryFindBestFoodSourceFor(servant, master, false, out food, out foodDef, true, true, false, false, true, false, false, false, false, false))
                        {
                            if (food != null)
                            {
                                return JobMaker.MakeJob(interaction.jobDef, food, master);
                            }
                        }
                        return null;
                    }

                    // 默认行为或RJW未激活时的回退行为：执行XML中定义的Job。
                    return JobMaker.MakeJob(interaction.jobDef, master, master.Position);
                }
            }
            return null;
        }
    }
}