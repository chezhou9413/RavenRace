using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace RavenRace.Compat.Epona
{
    public class CompProperties_EponaHybridLogic : CompProperties
    {
        public CompProperties_EponaHybridLogic()
        {
            this.compClass = typeof(Comp_EponaHybridLogic);
        }
    }

    /// <summary>
    /// 模拟艾波娜 JobHediff 机制的组件
    /// </summary>
    public class Comp_EponaHybridLogic : ThingComp
    {
        private Pawn Pawn => (Pawn)parent;

        // 每 30 ticks (0.5秒) 检查一次
        // 原版设定 60 ticks +0.06 severity
        // 我们改为 30 ticks +0.03 severity，效果等同
        private const int TicksToHediffMax = 60;
        private const float SeverityIncrease = 0.06f;

        private int ticksCounter = TicksToHediffMax;

        private static readonly HashSet<string> targetJobDefs = new HashSet<string>
        {
            "Goto", "Follow", "FollowClose", "FollowRoper", "GotoSafeTemperature",
            "GotoWander", "HaulToTransporter", "Flee", "FleeAndCower", "HaulToCell",
            "HaulToContainer", "Steal", "Kidnap", "CarryDownedPawnToExit", "Rescue",
            "TakeDownedPawnToBedDrafted", "CarryDownedPawnDrafted", "ReleasePrisoner",
            "EscortPrisonerToBed", "TakeWoundedPrisonerToBed", "TakeToBedToOperate", "Hunt"
        };

        /// <summary>
        /// 核心拦截：检查Mod是否激活、设置是否允许、角色是否有艾波娜血脉
        /// </summary>
        private bool IsActiveAndValid()
        {
            if (!RavenRaceMod.Settings.enableEponaCompat) return false;
            if (!EponaCompatUtility.IsEponaActive) return false;
            if (Pawn == null || !Pawn.Spawned || Pawn.Dead || Pawn.Downed) return false;

            return EponaCompatUtility.HasEponaBloodline(Pawn);
        }

        public override void CompTick()
        {
            base.CompTick();

            // 如果没有血脉，直接休眠，不执行任何消耗性能的逻辑
            if (!IsActiveAndValid()) return;

            if (!Pawn.IsHashIntervalTick(30)) return;

            // 1. 检查 Job
            if (Pawn.jobs?.curJob != null && targetJobDefs.Contains(Pawn.jobs.curJob.def.defName))
            {
                // 2. 检查室外 (无屋顶)
                bool isOutdoors = Pawn.GetRoom()?.PsychologicallyOutdoors ?? false;

                if (isOutdoors)
                {
                    ticksCounter -= 30;
                    if (ticksCounter <= 0)
                    {
                        ApplyHediff();
                        ticksCounter = TicksToHediffMax;
                    }
                    return;
                }
            }

            ticksCounter = TicksToHediffMax;
        }

        private void ApplyHediff()
        {
            // 始终使用通用加速 Hediff (三合一)
            if (EponaCompatUtility.EponaRunHediff == null) return;

            Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(EponaCompatUtility.EponaRunHediff);
            if (hediff == null)
            {
                hediff = Pawn.health.AddHediff(EponaCompatUtility.EponaRunHediff);
                hediff.Severity = 0.01f;
            }
            else
            {
                hediff.Severity = Mathf.Min(hediff.Severity + SeverityIncrease, 1f);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
        }
    }
}