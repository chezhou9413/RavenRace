using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.AI;
using System.Linq;

namespace RavenRace.Buildings.Comps
{
    /// <summary>
    /// 催情香炉的属性类，用于在XML中定义其效果范围和概率。
    /// </summary>
    public class CompProperties_IncenseBurner : CompProperties
    {
        /// <summary>
        /// 香炉效果的影响半径。
        /// </summary>
        public float effectRadius = 9.9f;
        /// <summary>
        /// 每秒为范围内的Pawn提供的娱乐值。
        /// </summary>
        public float joyAmount = 0.05f;
        /// <summary>
        /// 每次检测时，触发范围内渡鸦族使用“强制求爱”技能的概率。
        /// </summary>
        public float forceLovinChance = 0.05f;

        public CompProperties_IncenseBurner()
        {
            // 链接到逻辑实现类
            this.compClass = typeof(CompIncenseBurner);
        }
    }

    /// <summary>
    /// 催情香炉的核心逻辑组件。
    /// 负责周期性地检测周围环境，并对范围内的Pawn施加心情、娱乐和行为影响。
    /// </summary>
    public class CompIncenseBurner : ThingComp
    {
        // 获取属性的便捷方式
        public CompProperties_IncenseBurner Props => (CompProperties_IncenseBurner)props;
        // 缓存燃料组件的引用
        private CompRefuelable refuelable;

        /// <summary>
        /// 当组件被添加到地图上时调用，用于初始化。
        /// </summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // 获取并缓存该建筑的燃料组件
            refuelable = parent.GetComp<CompRefuelable>();
        }

        /// <summary>
        /// 组件的周期性更新方法。
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();
            // 使用设置中的检测间隔，并用IsHashIntervalTick来分散计算，提高性能
            if (parent.IsHashIntervalTick(RavenRaceMod.Settings.incenseCheckInterval))
            {
                DoEffect();
            }
        }

        /// <summary>
        /// 执行香炉的主要效果逻辑。
        /// </summary>
        private void DoEffect()
        {
            // 如果没有燃料，则不产生任何效果
            if (refuelable != null && !refuelable.HasFuel) return;

            Map map = parent.Map;
            List<Pawn> pawnsInRange = new List<Pawn>();

            // 1. 获取效果范围内的所有类人生物
            foreach (Thing t in GenRadial.RadialDistinctThingsAround(parent.Position, map, Props.effectRadius, true))
            {
                if (t is Pawn p && !p.Dead && !p.Downed && p.RaceProps.Humanlike)
                {
                    pawnsInRange.Add(p);
                }
            }

            if (pawnsInRange.Count == 0) return;

            // 2. 对每个范围内的Pawn施加效果
            foreach (Pawn p in pawnsInRange)
            {
                // A. 提供娱乐值和“迷醉香气”心情
                p.needs?.joy?.GainJoy(RavenRaceMod.Settings.incenseJoyAmount, JoyKindDefOf.Social);
                if (RavenBuildingDefOf.Raven_Thought_IncenseSmell != null)
                {
                    p.needs?.mood?.thoughts?.memories?.TryGainMemory(RavenBuildingDefOf.Raven_Thought_IncenseSmell);
                }

                // B. 施加独立的“香薰氛围”Hediff，提供轻微的正面状态加成
                HediffDef auraDef = DefDatabase<HediffDef>.GetNamedSilentFail("RavenHediff_IncenseAura");
                if (auraDef != null)
                {
                    HealthUtility.AdjustSeverity(p, auraDef, 0.01f);
                }
            }

            // 3. 尝试触发渡鸦族的特殊行为（强制求爱）
            TriggerRavenAbility(pawnsInRange);
        }

        /// <summary>
        /// 尝试让范围内的渡鸦族使用“强制求爱”技能。
        /// </summary>
        private void TriggerRavenAbility(List<Pawn> pawnsInRange)
        {
            // 筛选出可以施法的渡鸦
            List<Pawn> capableRavens = pawnsInRange
                .Where(p => p.def.defName == "Raven_Race" && !p.Drafted &&
                            p.abilities?.GetAbility(RavenDefOf.Raven_Ability_ForceLovin) != null)
                .ToList();

            // 如果有可施法的渡鸦，并且随机判定成功
            if (capableRavens.Count > 0 && Rand.Chance(RavenRaceMod.Settings.incenseForceLovinChance))
            {
                Pawn caster = capableRavens.RandomElement();

                // 检查施法者是否已经在忙于交配，避免打断
                if (IsBusyLovin(caster)) return;

                // 筛选出有效的求爱目标
                var validTargets = pawnsInRange.Where(t => t != caster && !t.Downed && !IsBusyLovin(t)).ToList();
                if (validTargets.Count == 0) return;

                // 优先选择伴侣或有好感的异性
                Pawn target = validTargets.FirstOrDefault(t => LovePartnerRelationUtility.LovePartnerRelationExists(caster, t));
                if (target == null)
                {
                    target = validTargets.Where(t => t.gender != caster.gender && caster.relations.OpinionOf(t) > 20).RandomElementWithFallback();
                }
                // 如果找不到，就随便选一个
                if (target == null) target = validTargets.RandomElement();

                if (target != null)
                {
                    // 分配工作
                    Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_ForceLovin, target);
                    caster.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                    // 视觉和消息反馈
                    FleckMaker.ThrowMetaIcon(caster.Position, caster.Map, FleckDefOf.Heart);
                    Messages.Message("Raven_Message_IncenseEffect".Translate(caster.LabelShort, target.LabelShort), caster, MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        /// <summary>
        /// 辅助方法，检查一个Pawn当前是否正在执行交配相关的Job。
        /// </summary>
        private bool IsBusyLovin(Pawn p)
        {
            if (p.CurJob == null) return false;
            return p.CurJob.def == JobDefOf.Lovin ||
                   p.CurJob.def == RavenDefOf.Raven_Job_ForceLovin;
        }
    }
}