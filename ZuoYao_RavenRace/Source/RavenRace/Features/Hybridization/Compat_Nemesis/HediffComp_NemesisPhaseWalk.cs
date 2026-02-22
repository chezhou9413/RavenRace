using System;
using System.Reflection; // 必须引用反射
using UnityEngine;
using Verse;
using Verse.AI; // 引用 AI
using Verse.Sound;
using RimWorld;
using HarmonyLib; // 引用 Harmony 工具

namespace RavenRace.Compat.Nemesis
{
    public class HediffCompProperties_NemesisPhaseWalk : HediffCompProperties
    {
        public int jumpCooldownTicks = 1200; // 20秒
        public float minDistanceToJump = 30f; // 30格

        public HediffCompProperties_NemesisPhaseWalk()
        {
            this.compClass = typeof(HediffComp_NemesisPhaseWalk);
        }
    }

    public class HediffComp_NemesisPhaseWalk : HediffComp
    {
        private HediffCompProperties_NemesisPhaseWalk Props => (HediffCompProperties_NemesisPhaseWalk)props;

        private int ticksSinceLastJump = 1200;

        // 缓存反射字段，用于获取私有的 PathEndMode
        private static FieldInfo peModeField;

        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (ticksSinceLastJump < Props.jumpCooldownTicks)
                {
                    return "充能: " + ((Props.jumpCooldownTicks - ticksSinceLastJump) / 60).ToString() + "s";
                }
                return "相位就绪";
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksSinceLastJump, "ticksSinceLastJump", 1200);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            // 1. 冷却期极速返回
            if (ticksSinceLastJump < Props.jumpCooldownTicks)
            {
                ticksSinceLastJump++;
                return;
            }

            // 2. 基础检查
            if (Pawn.Map == null || !Pawn.Spawned || Pawn.Downed || Pawn.Dead) return;
            if (Pawn.Drafted) return; // 征召禁用

            // 3. 必须处于移动状态
            if (!Pawn.pather.Moving || !Pawn.pather.Destination.IsValid) return;

            // 4. 触发检测 (刚冷却好 或 每秒一次)
            bool justReady = (ticksSinceLastJump == Props.jumpCooldownTicks);
            bool periodicCheck = Pawn.IsHashIntervalTick(60);

            if (justReady || periodicCheck)
            {
                CheckAndJump();
            }
        }

        private void CheckAndJump()
        {
            // 获取最终目的地
            IntVec3 finalDest = Pawn.pather.Destination.Cell;

            // 计算距离
            float distSqr = Pawn.Position.DistanceToSquared(finalDest);
            float thresholdSqr = Props.minDistanceToJump * Props.minDistanceToJump;

            // 距离不足，不触发
            if (distSqr < thresholdSqr) return;

            // 执行折跃
            DoPhaseJump(Pawn.pather.Destination);
        }

        private void DoPhaseJump(LocalTargetInfo originalDestination)
        {
            Map map = Pawn.Map;
            IntVec3 originalPos = Pawn.Position;
            IntVec3 targetCell = originalDestination.Cell;

            // --- 关键步骤 1：保存当前寻路状态 ---
            // 因为 Notify_Teleported 会调用 StopDead()，这会把 peMode 重置为 None。
            // 我们必须在传送前把这个模式存下来。
            if (peModeField == null)
            {
                peModeField = AccessTools.Field(typeof(Pawn_PathFollower), "peMode");
            }
            PathEndMode savedPeMode = (PathEndMode)peModeField.GetValue(Pawn.pather);

            // --- 关键步骤 2：寻找安全落脚点 ---
            IntVec3 safeDest = targetCell;

            // 如果目标点不可走（或者是墙壁/深水），尝试找附近的点
            // 注意：不能用 TryFindBestPawnStandCell，因为它用于找当前站立点
            if (!targetCell.Walkable(map) || targetCell.Fogged(map))
            {
                bool found = CellFinder.TryFindRandomCellNear(targetCell, map, 3,
                    c => c.Walkable(map) && !c.Fogged(map),
                    out safeDest);

                if (!found) return; // 找不到落脚点，取消
            }

            // --- 关键步骤 3：执行传送 ---

            // 起跳特效
            SpawnEffect(originalPos, map);

            // 移动坐标
            Pawn.Position = safeDest;

            // 通知系统传送发生了（这会清除当前路径并停止 Pawn）
            Pawn.Notify_Teleported(false, true);
            // 此时 Pawn.pather.Moving 变成了 false

            // 落地特效
            SpawnEffect(safeDest, map);

            // --- 关键步骤 4：无缝续接寻路逻辑 ---
            // 此时 Pawn 到了新位置，但大脑是空的。我们需要手动帮它“重启”移动。

            // 检查是否已经到达（比如对于 PathEndMode.Touch，相邻就算到达）
            if (Pawn.CanReachImmediate(originalDestination, savedPeMode))
            {
                // 如果已经满足到达条件，直接通知 Driver “我到了”
                // 这会让 Toil 结束，进入工作的下一个阶段
                Pawn.jobs.curDriver.Notify_PatherArrived();
            }
            else
            {
                // 如果还没到（可能稍微偏了一点，或者 peMode 是 OnCell 但我们落在了旁边），
                // 强制 Pather 重新计算从“当前新位置”到“原目标”的路径。
                try
                {
                    Pawn.pather.StartPath(originalDestination, savedPeMode);
                }
                catch (Exception ex)
                {
                    // 兜底防止红字卡死
                    Log.Warning($"[RavenRace] PhaseWalk path resume failed: {ex.Message}");
                    Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }

            // 重置冷却
            ticksSinceLastJump = 0;
        }

        private void SpawnEffect(IntVec3 pos, Map map)
        {
            if (NemesisCompatUtility.NemesisTeleportFleck != null)
            {
                FleckCreationData fcd = default(FleckCreationData);
                fcd.def = NemesisCompatUtility.NemesisTeleportFleck;
                fcd.spawnPosition = pos.ToVector3Shifted();
                fcd.scale = 3.5f;
                fcd.rotation = Rand.Range(0f, 360f);
                map.flecks.CreateFleck(fcd);
            }
            else
            {
                FleckMaker.ThrowSmoke(pos.ToVector3Shifted(), map, 2.0f);
                SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(pos, map));
            }
        }
    }
}