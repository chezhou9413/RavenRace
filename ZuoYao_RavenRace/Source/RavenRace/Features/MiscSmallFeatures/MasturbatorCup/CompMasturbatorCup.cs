using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.AI;
using System.Linq;

namespace RavenRace.Features.MiscSmallFeatures.MasturbatorCup
{
    public class CompProperties_MasturbatorCup : CompProperties
    {
        public CompProperties_MasturbatorCup()
        {
            this.compClass = typeof(CompMasturbatorCup);
        }
    }

    public class CompMasturbatorCup : ThingComp
    {
        // =========================================================
        // 数据字段：改为保存 ID 字符串，避免直接引用导致的存档崩溃
        // =========================================================
        private string boundTargetID;

        // 运行时缓存对象，避免每帧查找
        private Pawn cachedBoundTarget;

        // 封装属性：自动处理 ID 与对象的转换
        public Pawn BoundTarget
        {
            get
            {
                // 1. 如果缓存有效，直接返回
                if (cachedBoundTarget != null && !cachedBoundTarget.Destroyed) return cachedBoundTarget;

                // 2. 如果 ID 为空，说明没绑定
                if (string.IsNullOrEmpty(boundTargetID)) return null;

                // 3. 尝试通过 ID 查找 Pawn
                cachedBoundTarget = FindPawnByThingID(boundTargetID);

                // 4. 如果找到了但已经销毁/死亡，视为无效（可选：自动解绑）
                if (cachedBoundTarget != null && (cachedBoundTarget.Destroyed || cachedBoundTarget.Dead))
                {
                    // 这里我们暂不自动清空 ID，允许玩家看到"(失效)"的状态
                }

                return cachedBoundTarget;
            }
            set
            {
                cachedBoundTarget = value;
                boundTargetID = value?.ThingID;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            // [核心修复] 使用 Scribe_Values 保存字符串，绝对安全
            Scribe_Values.Look(ref boundTargetID, "boundTargetID");
        }

        // 全局查找 Pawn 的辅助方法
        private Pawn FindPawnByThingID(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            // 1. 当前地图
            if (Find.CurrentMap != null)
            {
                var p = Find.CurrentMap.mapPawns.AllPawns.FirstOrDefault(x => x.ThingID == id);
                if (p != null) return p;
            }

            // 2. 所有地图
            foreach (var map in Find.Maps)
            {
                var p = map.mapPawns.AllPawns.FirstOrDefault(x => x.ThingID == id);
                if (p != null) return p;
            }

            // 3. 世界 Pawn (包括被绑架、远行队等)
            if (Find.WorldPawns != null)
            {
                // 这是一个比较慢的操作，但通常只在加载或属性访问时调用一次
                // AllPawnsAliveOrDead 包含了所有非地图 Pawn
                // 注意：为了性能，我们这里只查活着的
                // 如果需要查死人，可以用 AllPawnsAliveOrDead
                // 这里我们假设目标应该活着
                // 但 WorldPawns 没有直接暴露简单的 List，通常用 PassToWorld 时的引用
                // 这里我们简单处理：如果在地图上找不到，暂不深究，或者遍历 WorldPawns.AllPawnsAlive
                // 实际上 WorldPawns 也是 IThingHolder，比较复杂，暂略。
                // 大多数情况下，目标都在某个地图上。
            }

            return null;
        }

        // =========================================================
        // 1. 右键菜单：允许捡起放入物品栏
        // =========================================================
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (parent.ParentHolder is Map)
            {
                if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
                {
                    yield return new FloatMenuOption("CannotReach".Translate(), null);
                    yield break;
                }

                yield return new FloatMenuOption("捡起 " + parent.Label, () =>
                {
                    Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, parent);
                    job.count = 1;
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }

        // =========================================================
        // 2. 检查面板信息
        // =========================================================
        public override string CompInspectStringExtra()
        {
            // 使用属性访问，如果已加载会自动解析
            Pawn target = BoundTarget;

            if (RavenRaceMod.Settings.enableDimensionalSex && !string.IsNullOrEmpty(boundTargetID))
            {
                string name = target?.LabelShort ?? "未知目标 (离线)";
                string status = (target == null || target.Dead || target.Destroyed) ? "(失效)" : "";
                return $"绑定目标: {name} {status}";
            }
            return null;
        }

        // =========================================================
        // 3. Gizmo 生成器 (供 Harmony Patch 调用)
        // =========================================================
        public IEnumerable<Gizmo> GetInventoryGizmos(Pawn holder)
        {
            if (!RavenRaceMod.Settings.enableDimensionalSex || !holder.IsColonistPlayerControlled) yield break;

            Pawn target = BoundTarget;

            // 分支 A: 未绑定或目标失效 (ID为空)
            if (string.IsNullOrEmpty(boundTargetID))
            {
                yield return new Command_Action
                {
                    defaultLabel = "绑定目标",
                    defaultDesc = "与地图上的任意目标建立量子纠缠连接。点击后用鼠标选择。",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Raven_InsertBeads", true),
                    action = () => StartTargeting(holder)
                };
            }
            // 分支 B: 已绑定
            else
            {
                // 检查目标有效性
                bool targetValid = target != null && !target.Dead && !target.Destroyed;

                // 按钮 1: 开始
                yield return new Command_Action
                {
                    defaultLabel = "开始次元性交",
                    defaultDesc = $"强制 {target?.LabelShort ?? "目标"} 进入高潮状态。\n\n再次点击此按钮执行，或使用右侧按钮取消绑定。",
                    icon = RavenDefOf.Raven_Ability_ForceLovin.uiIcon,
                    Disabled = !targetValid,
                    disabledReason = targetValid ? "" : "目标已死亡或消失",
                    action = () => StartDimensionalSex(holder)
                };

                // 按钮 2: 断开
                yield return new Command_Action
                {
                    defaultLabel = "断开连接",
                    defaultDesc = "切断当前的量子纠缠，允许重新绑定其他目标。",
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                    action = () =>
                    {
                        BoundTarget = null; // 清空
                        Messages.Message("连接已断开。", holder, MessageTypeDefOf.NeutralEvent);
                    }
                };
            }
        }

        // =========================================================
        // 4. 瞄准与执行逻辑
        // =========================================================
        private void StartTargeting(Pawn user)
        {
            TargetingParameters parms = new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetItems = false,
                canTargetLocations = false,
                canTargetSelf = false,
                validator = (TargetInfo t) =>
                {
                    Pawn p = t.Thing as Pawn;
                    return p != null && p.RaceProps.Humanlike;
                }
            };

            Find.Targeter.BeginTargeting(
                parms,
                (LocalTargetInfo target) =>
                {
                    Pawn p = target.Pawn;
                    if (p != null)
                    {
                        this.BoundTarget = p; // 保存
                        Messages.Message($"成功绑定目标: {p.LabelShort}", user, MessageTypeDefOf.TaskCompletion);
                    }
                },
                user,
                null,
                RavenDefOf.Raven_Ability_ForceLovin.uiIcon
            );
        }

        private void StartDimensionalSex(Pawn user)
        {
            Pawn target = BoundTarget;
            if (target == null) return;

            if (target.Map == null)
            {
                Messages.Message($"目标 {target.LabelShort} 当前不在任何地图上，无法连接。", user, MessageTypeDefOf.RejectInput);
                return;
            }

            Job victimJob = JobMaker.MakeJob(RavenDefOf.Raven_Job_DimensionalClimax, user);
            target.jobs.TryTakeOrderedJob(victimJob, JobTag.Misc);

            Job userJob = JobMaker.MakeJob(RavenDefOf.Raven_Job_MasturbateWithCup, parent);
            user.jobs.TryTakeOrderedJob(userJob, JobTag.Misc);

            Messages.Message($"{user.LabelShort} 激活了次元装置...", user, MessageTypeDefOf.NeutralEvent);
        }
    }
}