using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RavenRace.Features.DefenseSystem.Traps
{
    public class CompProperties_RavenBeastTrapLogic : CompProperties
    {
        public float lureRadius = 30f;
        public int checkInterval = 150; // [优化] 缩短间隔到 2.5秒，反应更灵敏

        public CompProperties_RavenBeastTrapLogic()
        {
            this.compClass = typeof(CompRavenBeastTrapLogic);
        }
    }

    /// <summary>
    /// 渡鸦捕兽夹核心逻辑组件
    /// </summary>
    public class CompRavenBeastTrapLogic : CompTrapEffect
    {
        public CompProperties_RavenBeastTrapLogic Props => (CompProperties_RavenBeastTrapLogic)props;

        private CompRefuelable refuelable;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelable = parent.GetComp<CompRefuelable>();
        }

        // ==========================================
        // 1. 诱饵逻辑 (TickRare -> 改为 Tick 计数以匹配自定义间隔)
        // ==========================================
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned) return;

            // 只有在有诱饵（燃料）时才工作
            if (refuelable == null || !refuelable.HasFuel) return;

            // 使用自定义的间隔进行检查
            if (parent.IsHashIntervalTick(Props.checkInterval))
            {
                DoLure();
            }
        }

        private void DoLure()
        {
            Map map = parent.Map;
            // 扫描范围内的所有生物
            foreach (Thing t in GenRadial.RadialDistinctThingsAround(parent.Position, map, Props.lureRadius, true))
            {
                if (t is Pawn p)
                {
                    if (IsValidTarget(p))
                    {
                        TryLurePawn(p);
                    }
                }
            }
        }

        private bool IsValidTarget(Pawn p)
        {
            // 必须是动物
            if (!p.RaceProps.Animal) return false;
            // 必须是野生
            if (p.Faction != null) return false;
            // 必须活着且未倒地 (发狂的动物也可以被吸引，这很合理)
            if (p.Dead || p.Downed) return false;

            // [野性检查] 必须未标记驯服
            if (parent.Map.designationManager.DesignationOn(p, DesignationDefOf.Tame) != null) return false;

            return true;
        }

        private void TryLurePawn(Pawn p)
        {
            // 1. 如果已经在执行走向该陷阱的任务，就不重复下达，避免鬼畜
            if (p.CurJobDef == JobDefOf.Goto && p.CurJob.targetA.Cell == parent.Position) return;

            // 2. [核心优化] 不再避让睡觉(LayDown)或进食状态。
            // 只有极少数状态（如正在被攻击并逃跑、极度精神崩溃）才避让。
            if (p.InMentalState && p.MentalStateDef != MentalStateDefOf.Manhunter) return; // 允许吸引猎杀人类的动物，但其他崩溃跳过
            if (p.jobs.curJob != null && p.jobs.curJob.def == JobDefOf.Flee) return; // 正在逃命就算了

            // 3. 路径检查
            if (p.CanReach(parent, PathEndMode.OnCell, Danger.Deadly))
            {
                // 创建任务：去陷阱的位置
                Job job = JobMaker.MakeJob(JobDefOf.Goto, parent.Position);

                // [关键修复 1] 大幅增加超时时间 (5000 ticks = 83秒)
                // 确保动物就算爬也能爬过来，不会半路放弃
                job.expiryInterval = 5000;

                // [关键修复 2] 开启强制过期检查，防止卡死
                job.checkOverrideOnExpire = true;

                // [关键修复 3] 启用小跑 (Jog)，模拟被诱饵强烈吸引
                job.locomotionUrgency = LocomotionUrgency.Jog;

                job.collideWithPawns = true;

                // [关键修复 4] 强行打断当前任务 (InterruptForced)
                p.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                // 可选：给个小气泡反馈
                if (p.IsHashIntervalTick(300))
                {
                    // [修复] 使用 FleckMaker
                    FleckMaker.ThrowMetaIcon(p.Position, p.Map, FleckDefOf.IncapIcon);
                }
            }
        }

        // ==========================================
        // 2. 击杀逻辑 (OnTriggered)
        // ==========================================
        public override void OnTriggered(Pawn triggerer)
        {
            // 双重检查燃料
            if (refuelable == null || !refuelable.HasFuel) return;

            if (triggerer == null) return;

            // 1. 消耗一次诱饵
            refuelable.ConsumeFuel(1.0f);

            // 2. 播放音效
            SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(parent.Position, parent.Map));

            // 3. 执行完美击杀
            if (!triggerer.Dead)
            {
                // Kill(null) 代表不指定伤害来源，通常会保留全尸
                triggerer.Kill(null, null);
            }

            // 4. 处理尸体
            if (triggerer.Corpse != null)
            {
                triggerer.Corpse.SetForbidden(false);
            }

            // 5. 消息提示
            Messages.Message("Raven_Message_BeastTrapKill".Translate(triggerer.LabelShort), triggerer.Corpse, MessageTypeDefOf.NeutralEvent);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            if (RavenRaceMod.Settings.enableDefenseSystemDebug)
            {
                GenDraw.DrawRadiusRing(parent.Position, Props.lureRadius);
            }
        }
    }
}