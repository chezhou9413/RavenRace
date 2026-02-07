using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace
{
    public class CompProperties_RavenTaunt : CompProperties
    {
        public float tauntRadius = 25f;
        public int tauntInterval = 120;
        public bool forceJob = true;

        public CompProperties_RavenTaunt()
        {
            this.compClass = typeof(CompRavenTaunt);
        }
    }

    public class CompRavenTaunt : ThingComp
    {
        public CompProperties_RavenTaunt Props => (CompProperties_RavenTaunt)this.props;

        public override void CompTick()
        {
            base.CompTick();
            // 必须 Spawned 且有 Map
            if (parent.Spawned && parent.Map != null && parent.IsHashIntervalTick(Props.tauntInterval))
            {
                DoTaunt();
            }
        }

        private void DoTaunt()
        {
            List<Pawn> enemies = new List<Pawn>();
            // 使用 RadialDistinctThingsAround 可能会比较慢，但对于 2秒一次 是可以接受的
            foreach (Thing t in GenRadial.RadialDistinctThingsAround(parent.Position, parent.Map, Props.tauntRadius, true))
            {
                if (t is Pawn p && !p.Dead && !p.Downed && p.HostileTo(parent.Faction))
                {
                    // 假人需要视线
                    if (parent.def == DefenseDefOf.RavenDecoy_Dummy)
                    {
                        if (!GenSight.LineOfSight(parent.Position, p.Position, parent.Map)) continue;
                    }
                    enemies.Add(p);
                }
            }

            foreach (Pawn p in enemies)
            {
                // 1. 修改 AI 目标
                p.mindState.enemyTarget = parent;

                // 2. 强行打断并给予攻击任务
                if (Props.forceJob)
                {
                    // 检查是否已经在攻击我
                    if (p.CurJob != null && p.CurJob.targetA.Thing == parent &&
                       (p.CurJob.def == JobDefOf.AttackMelee || p.CurJob.def == JobDefOf.AttackStatic))
                    {
                        continue;
                    }

                    // [新增] 检查是否可达，不可达就不强求，避免 AI 报错或卡顿
                    if (!p.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
                    {
                        continue;
                    }

                    // 给予近战攻击任务，迫使移动
                    Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, parent);
                    job.expiryInterval = 300; // 5秒
                    job.checkOverrideOnExpire = true;
                    job.collideWithPawns = true;
                    // 设置 source 为该 Thing，方便调试
                    job.playerForced = true;

                    // 强力打断
                    p.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                    // 视觉反馈
                    FleckMaker.ThrowMetaIcon(p.Position, p.Map, FleckDefOf.IncapIcon);
                }
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            if (RavenRaceMod.Settings.enableDefenseSystemDebug)
            {
                GenDraw.DrawRadiusRing(parent.Position, Props.tauntRadius);
            }
        }
    }
}