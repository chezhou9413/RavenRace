using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;
using System.Linq; // 确保引用
using System.Collections.Generic; // 确保引用

namespace RavenRace
{
    public class RavenGas : Gas
    {
        protected override void Tick()
        {
            if (this.Destroyed) return;

            if (this.IsHashIntervalTick(60))
            {
                destroyTick -= 60;
                if (destroyTick <= Find.TickManager.TicksGame)
                {
                    this.Destroy(DestroyMode.Vanish);
                    return;
                }
                DoGasEffect();
            }

            if (this.Graphic != null)
            {
                this.Graphic.DrawWorker(this.DrawPos, this.Rotation, this.def, this, 0f);
            }
        }

        private void DoGasEffect()
        {
            var things = this.Map.thingGrid.ThingsListAt(this.Position);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn p) ApplyEffects(p);
            }
        }

        private void ApplyEffects(Pawn p)
        {
            if (p.Dead) return;

            if (this.def == DefenseDefOf.RavenGas_Anesthetic)
            {
                HealthUtility.AdjustSeverity(p, DefenseDefOf.RavenHediff_AnestheticBuildup, 0.05f);
            }
            else if (this.def == DefenseDefOf.RavenGas_Aphrodisiac)
            {
                HealthUtility.AdjustSeverity(p, DefenseDefOf.RavenHediff_AphrodisiacEffect, 0.05f);
                TryStartForcedLovin(p);
            }
        }

        /// <summary>
        /// 尝试为Pawn分配强制“爱爱”任务。
        /// 已重构以增强健壮性，防止多陷阱触发时的崩溃。
        /// </summary>
        /// <param name="p">触发气体效果的Pawn</param>
        private void TryStartForcedLovin(Pawn p)
        {
            // 状态检查
            if (p.Downed || p.CurJobDef == JobDefOf.Lovin || p.CurJobDef == RavenDefOf.Raven_Job_ForceLovin) return;

            // 核心修改：获取一个全新的、独立的Pawn列表副本。
            // 这可以防止在迭代时列表被外部（如乐高陷阱）修改，从而避免崩溃。
            List<Pawn> potentialPartners = GenRadial.RadialDistinctThingsAround(p.Position, p.Map, 13f, true)
                                                    .OfType<Pawn>()
                                                    .Where(other => other != p && !other.Downed && !other.Dead && other.RaceProps.Humanlike && !other.HostileTo(p))
                                                    .ToList(); // ToList() 创建一个新列表

            // 如果没有有效目标，则让Pawn徘徊
            if (potentialPartners.NullOrEmpty())
            {
                if (p.CurJobDef != JobDefOf.GotoWander)
                {
                    Job wander = JobMaker.MakeJob(JobDefOf.GotoWander, p.Position);
                    p.jobs.StartJob(wander, JobCondition.InterruptForced);
                }
                return;
            }

            // 从列表中随机选择一个伴侣
            Pawn target = potentialPartners.RandomElement();

            // 分配任务
            Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_ForceLovin, target);
            p.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}