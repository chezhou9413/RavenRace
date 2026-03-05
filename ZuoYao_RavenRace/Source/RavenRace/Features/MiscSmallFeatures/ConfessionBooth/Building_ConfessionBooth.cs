using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using System.Text;

namespace RavenRace.Features.MiscSmallFeatures.ConfessionBooth
{
    [StaticConstructorOnStartup]
    /// <summary>
    /// 忏悔室建筑核心类。
    /// 继承自 Building_Enterable 以获得"保活"地图（防止地图因无活动而卸载）的能力。
    /// 工作流程：
    /// 1. 玩家指定修女，然后从菜单选择信徒
    /// 2. 两人被分配 Job，走向忏悔室
    /// 3. 先到的人等待（通过 waitingTicks 超时机制防止永久等待）
    /// 4. 两人都进入后开始倒计时（ticksRemaining）
    /// 5. 完成后触发 FinishConfession，弹出两人并施加效果
    /// </summary>
    public class Building_ConfessionBooth : Building_Enterable
    {
        // -------------------------------------------------------
        // 持久化字段（需要存档）
        // -------------------------------------------------------

        /// <summary>剩余忏悔时间（游戏 Tick 数）。两人都进入后开始倒计时。</summary>
        private int ticksRemaining = 0;

        /// <summary>
        /// 等待计时器：当容器内只有 1 人时开始累积。
        /// 超过阈值后认为同伴无法到达，强制弹出并取消仪式。
        /// </summary>
        private int waitingTicks = 0;

        /// <summary>
        /// 标记忏悔是否已"启动"（两人都进入，ticksRemaining 已被正确设置）。
        /// 防止 ticksRemaining 被提前设置导致的竞态问题。
        /// </summary>
        private bool sessionStarted = false;

        // -------------------------------------------------------
        // 静态图标资源（不需要存档）
        // -------------------------------------------------------

        /// <summary>取消/终止按钮图标</summary>
        private static readonly Texture2D CancelIcon =
            ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);

        /// <summary>插入/选择人员按钮图标，找不到时回退到取消图标</summary>
        private static readonly Texture2D InsertIcon =
            ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true)
            ?? ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);

        // -------------------------------------------------------
        // 生命周期方法
        // -------------------------------------------------------

        /// <summary>
        /// 建筑在地图上生成后调用。
        /// 设置 innerContainer 不自动 Tick 内部 Pawn（防止被挂起的 Pawn 执行 Job 造成异常）。
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // 禁止容器自动 Tick 内部内容，Pawn 在容器内处于"冻结"状态
            if (this.innerContainer != null)
            {
                this.innerContainer.dontTickContents = true;
            }
        }

        /// <summary>
        /// 存档读写。确保所有持久化字段被正确保存和加载。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
            Scribe_Values.Look(ref waitingTicks, "waitingTicks", 0);
            Scribe_Values.Look(ref sessionStarted, "sessionStarted", false);

            // 读档后重新设置容器属性，防止存档丢失该标志
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.innerContainer != null)
            {
                this.innerContainer.dontTickContents = true;
            }
        }

        // -------------------------------------------------------
        // 核心 Tick 逻辑
        // -------------------------------------------------------

        /// <summary>
        /// 建筑每帧 Tick（TickerType.Normal 时每帧调用）。
        /// 负责：
        /// - 检测两人是否都已进入，若是则启动倒计时
        /// - 执行倒计时并定期生成爱心特效
        /// - 检测单人等待超时并强制取消
        /// </summary>
        protected override void Tick()
        {
            base.Tick();

            if (innerContainer.Count == 2)
            {
                // 两人都进入了容器，重置等待计时器
                waitingTicks = 0;

                if (!sessionStarted)
                {
                    // 【核心修复】在两人都进入容器后才设置 ticksRemaining。
                    // 之前在 TryStartConfession 里提前设置，存在竞态风险。
                    // 现在改为在此处延迟设置，确保时机正确。
                    ticksRemaining = (int)(RavenRaceMod.Settings.confessionDurationHours * 2500f);
                    sessionStarted = true;
                }

                if (ticksRemaining > 0)
                {
                    ticksRemaining--;

                    // 每 120 tick 生成一次爱心特效
                    if (this.IsHashIntervalTick(120))
                    {
                        FleckMaker.ThrowMetaIcon(this.Position, this.Map, FleckDefOf.Heart, 0.5f);
                    }

                    // 倒计时归零，触发忏悔完成
                    if (ticksRemaining <= 0)
                    {
                        FinishConfession();
                    }
                }
            }
            else if (innerContainer.Count == 1)
            {
                // 只有一人在容器内，等待同伴到来
                waitingTicks++;

                // 超过约 2 分钟游戏时间（5000 ticks）仍未等到同伴，取消仪式
                if (waitingTicks > 5000)
                {
                    EjectAll("同伴迟迟未到达，取消了忏悔。");
                    waitingTicks = 0;
                }
            }
            else
            {
                // 容器为空，重置等待计时器
                waitingTicks = 0;
            }
        }

        // -------------------------------------------------------
        // Building_Enterable 抽象方法实现
        // -------------------------------------------------------

        /// <summary>
        /// 检查一个 Pawn 是否可以被忏悔室接受。
        /// 条件：容器未满，且 Pawn 是殖民者/囚犯/奴隶。
        /// </summary>
        public override AcceptanceReport CanAcceptPawn(Pawn p)
        {
            if (innerContainer.Count >= 2)
            {
                return "忏悔室已满。";
            }
            if (p.IsColonist || p.IsPrisonerOfColony || p.IsSlaveOfColony)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 将一个 Pawn 接收进容器。
        /// 先将 Pawn 从地图上移除（DeSpawnOrDeselect），再加入内部容器。
        /// </summary>
        public override void TryAcceptPawn(Pawn p)
        {
            // 二次检查：容器已满则直接返回
            if (innerContainer.Count >= 2) return;

            // 将 Pawn 从地图移除（若被选中则同时取消选中）
            bool wasSelected = p.DeSpawnOrDeselect(DestroyMode.Vanish);

            // 尝试将 Pawn 加入容器
            innerContainer.TryAddOrTransfer(p, true);

            // 如果 Pawn 原本被选中，重新选中它（维持原版行为，如 Building_GeneExtractor）
            if (wasSelected)
            {
                Find.Selector.Select(p, false, false);
            }
        }

        /// <summary>
        /// Building_Enterable 要求实现的抽象属性：容器内 Pawn 的绘制偏移。
        /// 忏悔室不需要显示内部 Pawn，返回零向量。
        /// </summary>
        public override Vector3 PawnDrawOffset => Vector3.zero;

        // -------------------------------------------------------
        // Gizmo（操作按钮）
        // -------------------------------------------------------

        /// <summary>
        /// 返回选中忏悔室时右下角显示的操作按钮。
        /// - 仪式进行中：显示"终止忏悔"按钮
        /// - 空闲且已指定修女：显示"选择忏悔者"按钮
        /// </summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // 先输出基类 Gizmo（包含 CompAssignableToPawn 的"指定修女"按钮）
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (innerContainer.Count > 0)
            {
                // 仪式进行中，显示终止按钮
                yield return new Command_Action
                {
                    defaultLabel = "终止忏悔",
                    defaultDesc = "立刻将里面的人拉出来中止仪式。",
                    icon = CancelIcon,
                    action = () => EjectAll("被手动终止。")
                };
            }
            else
            {
                // 空闲状态，若已指定修女则显示选择信徒按钮
                CompAssignableToPawn_Nun assignComp = GetComp<CompAssignableToPawn_Nun>();
                Pawn nun = assignComp?.AssignedPawnsForReading.FirstOrDefault();

                if (nun != null)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "选择忏悔者...",
                        defaultDesc = string.Format("当前修女：{0}\n点击选择一名非征召状态的异性进行肉欲忏悔。", nun.LabelShort),
                        icon = InsertIcon,
                        action = OpenConfessorMenu
                    };
                }
            }
        }

        // -------------------------------------------------------
        // 菜单与仪式启动
        // -------------------------------------------------------

        /// <summary>
        /// 打开选择忏悔者的浮动菜单。
        /// 列出地图上所有符合条件的异性（非修女、非倒地、非精神崩溃、非征召状态）。
        /// </summary>
        private void OpenConfessorMenu()
        {
            CompAssignableToPawn_Nun assignComp = GetComp<CompAssignableToPawn_Nun>();
            Pawn nun = assignComp?.AssignedPawnsForReading.FirstOrDefault();
            if (nun == null) return;

            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // 检查修女自身状态是否可以履行职责
            if (nun.Downed || nun.Dead || nun.InMentalState || nun.Drafted)
            {
                options.Add(new FloatMenuOption(
                    string.Format("修女 {0} 当前无法履行职责（倒地/崩溃/被征召）", nun.LabelShort),
                    null));
                Find.WindowStack.Add(new FloatMenu(options));
                return;
            }

            // 遍历地图上所有可能的忏悔者候选人
            foreach (Pawn p in Map.mapPawns.AllPawnsSpawned)
            {
                // 只允许殖民者、囚犯、奴隶
                if (!p.IsColonist && !p.IsPrisonerOfColony && !p.IsSlaveOfColony)
                    continue;

                // 排除修女自身和同性
                if (p == nun || p.gender == nun.gender)
                    continue;

                if (p.Downed || p.Dead || p.InMentalState || p.Drafted)
                {
                    options.Add(new FloatMenuOption(
                        string.Format("{0}：无法行动或处于征召状态", p.LabelShortCap),
                        null));
                    continue;
                }

                if (!p.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    options.Add(new FloatMenuOption(
                        string.Format("{0}：无法到达忏悔室", p.LabelShortCap),
                        null));
                    continue;
                }

                Pawn localPawn = p;
                options.Add(new FloatMenuOption(
                    localPawn.LabelShortCap,
                    () => TryStartConfession(localPawn, nun)));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("没有可用的异性忏悔者", null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        /// <summary>
        /// 启动忏悔仪式：分别给信徒和修女派发"进入忏悔室"的 Job。
        /// 注意：此处不再设置 ticksRemaining，改为在两人都进入容器后（Tick 中）设置，
        /// 彻底解决竞态问题。
        /// </summary>
        private void TryStartConfession(Pawn believer, Pawn nun)
        {
            // 重置会话状态，确保下次两人进入后重新计算时长
            sessionStarted = false;
            ticksRemaining = 0;
            waitingTicks = 0;

            // 给信徒派发进入 Job
            Job jobBeliever = JobMaker.MakeJob(RavenDefOf.Raven_Job_EnterConfessionBooth, this);
            believer.jobs.TryTakeOrderedJob(jobBeliever, JobTag.Misc);

            // 给修女派发进入 Job
            Job jobNun = JobMaker.MakeJob(RavenDefOf.Raven_Job_EnterConfessionBooth, this);
            nun.jobs.TryTakeOrderedJob(jobNun, JobTag.Misc);

            Messages.Message(
                string.Format("{0} 请求了一次忏悔，{1} 正在前往忏悔室。",
                    believer.LabelShort, nun.LabelShort),
                this,
                MessageTypeDefOf.NeutralEvent);
        }

        // -------------------------------------------------------
        // 仪式完成与弹出逻辑
        // -------------------------------------------------------

        /// <summary>
        /// 忏悔仪式完成时调用。
        /// 为双方施加 Hediff 和 Thought，并为信徒移除一个随机负面记忆。
        /// 完成后弹出两人。
        /// </summary>
        private void FinishConfession()
        {
            CompAssignableToPawn_Nun assignComp = GetComp<CompAssignableToPawn_Nun>();
            Pawn nun = assignComp?.AssignedPawnsForReading.FirstOrDefault();

            // 找到容器内不是修女的那个人（信徒）
            Pawn believer = innerContainer.OfType<Pawn>().FirstOrDefault(p => p != nun);

            if (nun != null && believer != null)
            {
                // 为信徒施加"肉欲净罪" Hediff
                if (RavenDefOf.Raven_Hediff_PurifiedByLust != null)
                {
                    believer.health.AddHediff(RavenDefOf.Raven_Hediff_PurifiedByLust);
                }

                // 为修女施加"罪恶容器" Hediff
                if (RavenDefOf.Raven_Hediff_NunReceptacle != null)
                {
                    nun.health.AddHediff(RavenDefOf.Raven_Hediff_NunReceptacle);
                }

                // 为信徒添加正面心情记忆
                if (RavenDefOf.Raven_Thought_ConfessedSin != null)
                {
                    believer.needs?.mood?.thoughts?.memories?.TryGainMemory(
                        RavenDefOf.Raven_Thought_ConfessedSin);
                }

                // 为修女添加正面心情记忆
                if (RavenDefOf.Raven_Thought_AbsorbedSin != null)
                {
                    nun.needs?.mood?.thoughts?.memories?.TryGainMemory(
                        RavenDefOf.Raven_Thought_AbsorbedSin);
                }

                // 为信徒移除一个随机负面记忆
                RemoveRandomNegativeMemory(believer);

                // 完成消息（使用 string.Format 避免中文引号在字符串插值中造成编译错误）
                Messages.Message(
                    string.Format(
                        "{0} 将浓浊的罪恶尽数射入了 {1} 体内，感到了前所未有的空灵与解脱。",
                        believer.LabelShort, nun.LabelShort),
                    new LookTargets(new Pawn[] { believer, nun }),
                    MessageTypeDefOf.PositiveEvent);
            }

            // 弹出所有人，不显示取消消息
            EjectAll(null);
        }

        /// <summary>
        /// 从 Pawn 的记忆中随机移除一个负面记忆（MoodOffset 小于 0 的记忆）。
        /// 移除成功后在 Pawn 头顶显示提示文字。
        /// </summary>
        private void RemoveRandomNegativeMemory(Pawn pawn)
        {
            var handler = pawn.needs?.mood?.thoughts?.memories;
            if (handler == null) return;

            // 找出所有负面且尚未标记为丢弃的记忆
            var negMemories = handler.Memories
                .Where(m => m.MoodOffset() < 0 && !m.ShouldDiscard)
                .ToList();

            if (negMemories.Any())
            {
                var toRemove = negMemories.RandomElement();
                handler.RemoveMemory(toRemove);
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "负面记忆已消除", Color.cyan);
            }
        }

        /// <summary>
        /// 强制弹出容器内所有 Pawn，并重置所有仪式状态。
        /// </summary>
        /// <param name="reason">显示给玩家的原因消息，为 null 则不显示。</param>
        public void EjectAll(string reason)
        {
            if (innerContainer.Count > 0)
            {
                // 将容器内所有 Pawn 放回交互格附近
                innerContainer.TryDropAll(
                    this.InteractionCell, this.Map, ThingPlaceMode.Near,
                    null, null, true);

                if (!string.IsNullOrEmpty(reason))
                {
                    Messages.Message(reason, this, MessageTypeDefOf.RejectInput);
                }
            }

            // 重置所有仪式状态
            ticksRemaining = 0;
            waitingTicks = 0;
            sessionStarted = false;
        }

        // -------------------------------------------------------
        // 其他重写
        // -------------------------------------------------------

        /// <summary>
        /// 建筑被摧毁时，先弹出所有人再执行销毁逻辑。
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            EjectAll("建筑被摧毁，人员被强制弹出。");
            base.Destroy(mode);
        }

        /// <summary>
        /// 返回选中建筑时底部检查面板显示的字符串。
        /// 显示当前仪式状态和剩余时间。
        /// </summary>
        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder(base.GetInspectString());

            if (innerContainer.Count > 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("当前状态：");

                if (innerContainer.Count == 1)
                {
                    sb.Append("等待同伴到达...");
                }
                else if (sessionStarted && ticksRemaining > 0)
                {
                    // 两人都进入且倒计时已启动，显示剩余时间
                    sb.Append(string.Format(
                        "狂热忏悔中（{0}）",
                        ticksRemaining.ToStringTicksToPeriod()));
                }
                else
                {
                    sb.Append("仪式准备中...");
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}