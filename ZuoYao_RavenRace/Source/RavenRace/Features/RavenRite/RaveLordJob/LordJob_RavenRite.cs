using RavenRace.Features.RavenRite.Pojo;
using RavenRace.Features.RavenRite.UnityEff;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RavenRace.Features.RavenRite.RaveLordJob
{
    //渡鸦仪式主Lord,分三阶段：聚集 执行读条 结束
    public class LordJob_RavenRite : LordJob
    {
        public IntVec3 altarSpot;
        public Pawn host;
        public List<Pawn> participants = new List<Pawn>();
        public int durationTicks = 2500;
        public string ritualLabel = "渡鸦仪式";
        public int circleRadius = 4;
        public Action<bool> OnFinished;
        public LordJob_RavenRite() { }
        public LordJob_RavenRite(
            IntVec3 altarSpot,
            Pawn host,
            List<Pawn> participants,
            int durationTicks,
            string ritualLabel,
            int circleRadius = 4,
            Action<bool> onFinished = null)
        {
            this.altarSpot = altarSpot;
            this.host = host;
            this.participants = participants ?? new List<Pawn>();
            this.durationTicks = durationTicks;
            this.ritualLabel = ritualLabel;
            this.circleRadius = circleRadius;
            this.OnFinished = onFinished;
        }

        private bool HostIsAtPosition(LordToil_RavenRite_Gather gatherToil)
        {
            if (host == null || !host.Spawned || host.Dead || host.Downed)
                return false;
            return host.Position == gatherToil.HostStandCell;
        }
        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();
            //Gather阶段直接分配最终 duty，Pawn 一开始就走向正确位置，不会二次重定向
            var gatherToil = new LordToil_RavenRite_Gather(altarSpot, host, circleRadius);
            var performToil = new LordToil_RavenRite_Perform(
                altarSpot,
                host, 
                durationTicks,
                ritualLabel, 
                circleRadius,
                 ritualTexts: new List<string> { "哈哈", "那就厉害了", "到时候火药给你留着，把那些黑基地全炸了", "好主意", "算我一个" });
            var finishToil = new LordToil_RavenRite_Finish();
            graph.StartingToil = gatherToil;
            graph.AddToil(performToil);
            graph.AddToil(finishToil);
            var toPerform = new Transition(gatherToil, performToil);
            toPerform.AddTrigger(new Trigger_TickCondition(() => AllPawnsInGatheringArea() && HostIsAtPosition(gatherToil), 60));
            graph.AddTransition(toPerform);
            //读条完成仪式成功
            var toFinishSuccess = new Transition(performToil, finishToil);
            toFinishSuccess.AddTrigger(new Trigger_Memo(LordToil_RavenRite_Perform.MemoSuccess));
            toFinishSuccess.postActions.Add(new TransitionAction_Custom(() =>
            {
                OnFinished?.Invoke(false);
                Messages.Message(ritualLabel + " 圆满完成。", host, MessageTypeDefOf.PositiveEvent, false);
            }));
            graph.AddTransition(toFinishSuccess);
            //晋升者受伤/倒地/精神崩溃仪式中断
            var toFinishFail = new Transition(gatherToil, finishToil);
            toFinishFail.AddSource(performToil);
            toFinishFail.AddTrigger(new Trigger_PawnHarmed(1f, requireInstigatorWithFaction: false));
            toFinishFail.AddTrigger(new Trigger_TickCondition(
                () => host == null || host.Dead || host.Downed || host.InMentalState));
            toFinishFail.postActions.Add(new TransitionAction_Custom(() =>
            {
                OnFinished?.Invoke(true);
                Messages.Message(ritualLabel + " 被打断，仪式失败。", host, MessageTypeDefOf.NegativeEvent, false);
            }));
            graph.AddTransition(toFinishFail);
            return graph;
        }

        //原Dead/Downed/未生成的Pawn跳过不阻塞
        private bool AllPawnsInGatheringArea()
        {
            foreach (var pawn in lord.ownedPawns)
            {
                if (pawn.Dead || pawn.Downed || !pawn.Spawned) continue;
                if (!GatheringsUtility.InGatheringArea(pawn.Position, altarSpot, lord.Map))
                    return false;
            }
            return true;
        }

        public override bool ShouldRemovePawn(Pawn p, PawnLostCondition reason) => true;
        public override bool EndPawnJobOnCleanup(Pawn p) => true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref altarSpot, "altarSpot");
            Scribe_Values.Look(ref durationTicks, "durationTicks", 2500);
            Scribe_Values.Look(ref ritualLabel, "ritualLabel", "渡鸦仪式");
            Scribe_Values.Look(ref circleRadius, "circleRadius", 4);
            Scribe_References.Look(ref host, "host");
            Scribe_Collections.Look(ref participants, "participants", LookMode.Reference);
        }
    }
    //阶段1：聚集
    public class LordToil_RavenRite_Gather : LordToil
    {
        private readonly IntVec3 spot;
        private readonly Pawn host;
        private readonly int circleRadius;
        private static DutyDef SpectateCircleDef => DefDatabase<DutyDef>.GetNamed("SpectateCircle");
        private static DutyDef GotoDef => DefDatabase<DutyDef>.GetNamed("Goto");
        public IntVec3 HostStandCell { get; private set; } = IntVec3.Invalid;

        public LordToil_RavenRite_Gather(IntVec3 spot, Pawn host, int circleRadius)
        {
            this.spot = spot;
            this.host = host;
            this.circleRadius = circleRadius;
        }

        public override void Init()
        {
            HostStandCell = CellFinder.StandableCellNear(spot, lord.Map, 3f);
            if (!HostStandCell.IsValid)HostStandCell = spot;
        }

        public override void UpdateAllDuties()
        {
            var centerRect = CellRect.SingleCell(spot);
            foreach (var pawn in lord.ownedPawns)
            {
                if (pawn == host)
                    pawn.mindState.duty = new PawnDuty(GotoDef, HostStandCell);
                else
                    pawn.mindState.duty = new PawnDuty(SpectateCircleDef, spot)
                    {
                        spectateRect = centerRect,
                        spectateDistance = new IntRange(circleRadius, circleRadius + 2)
                    };

                // 原版必须调用：强制 Pawn 在本 tick 立刻响应新 duty，而非等待下一个 job 检查周期
                pawn.jobs?.CheckForJobOverride();
            }
        }
        public override ThinkTreeDutyHook VoluntaryJoinDutyHookFor(Pawn p)
            => ThinkTreeDutyHook.HighPriority;
    }

    //存档数据：记录已过 tick 数，支持读档继续读条
    public class LordToilData_RavenRitePerform : LordToilData
    {
        public int ticksPassed;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref ticksPassed, "ticksPassed", 0);
        }
    }

    // 阶段2：晋升者站中心读条，参与者围圈旁观
    public class LordToil_RavenRite_Perform : LordToil
    {
        public const string MemoSuccess = "RavenRite_Success";
        private readonly IntVec3 spot;
        private readonly Pawn host;
        private readonly int totalTicks;
        private readonly string label;
        private readonly int circleRadius;
        private static DutyDef SpectateCircleDef => DefDatabase<DutyDef>.GetNamed("SpectateCircle");
        private static DutyDef GotoDef => DefDatabase<DutyDef>.GetNamed("Goto");
        private CellRect centerRect;
        private IntVec3 hostStandCell;
        private Effecter progressBar;
        private RaveMagicArea magicAreaEffect;
        //通过 LordToilData 子类持久化 ticksPassed，读档后自动恢复
        private readonly List<string> ritualTexts; // 传入的文本集合
        private int lastSpeakerIndex = -1;         // 上次说话的pawn索引，避免连续同一人
        private int nextSpeakTick = 0;             // 下次触发说话的tick
        private int speakInterval = 300;           // 每隔多少tick说一次
        private int speakCount = 0;
        private LordToilData_RavenRitePerform PerformData
        {
            get
            {
                if (data == null) data = new LordToilData_RavenRitePerform();
                return (LordToilData_RavenRitePerform)data;
            }
        }

        private int TicksPassed
        {
            get => PerformData.ticksPassed;
            set => PerformData.ticksPassed = value;
        }

        public LordToil_RavenRite_Perform(
            IntVec3 spot, Pawn host, int totalTicks, string label, int circleRadius = 4, List<string> ritualTexts = null)
        {
            this.spot = spot;
            this.host = host;
            this.totalTicks = totalTicks;
            this.label = label;
            this.circleRadius = circleRadius;
            this.ritualTexts = ritualTexts ?? new List<string>();
        }

        public override void Init()
        {
            centerRect = CellRect.SingleCell(spot);
            hostStandCell = CellFinder.StandableCellNear(spot, lord.Map, 3f);
            if (!hostStandCell.IsValid) hostStandCell = spot;
            magicAreaEffect = new RaveMagicArea();
            magicAreaEffect.Spawn(spot.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead));
        }

        public override void Cleanup()
        {
            progressBar?.Cleanup();
            progressBar = null;
            magicAreaEffect?.Destroy();
            magicAreaEffect = null;
        }

        public override void LordToilTick()
        {
            TicksPassed++;
            if (ritualTexts != null && ritualTexts.Count > 0 && TicksPassed >= nextSpeakTick)
            {
                nextSpeakTick = TicksPassed + speakInterval;

                var speakers = lord.ownedPawns
                    .Where(p => p.Spawned && !p.Dead && !p.Downed)
                    .ToList();

                if (speakers.Count > 0)
                {
                    Pawn speaker;
                    if (speakers.Count == 1)
                    {
                        speaker = speakers[0];
                    }
                    else
                    {
                        var candidates = speakers
                            .Where((p, i) => i != lastSpeakerIndex)
                            .ToList();
                        int idx = Rand.Range(0, candidates.Count);
                        speaker = candidates[idx];
                        lastSpeakerIndex = speakers.IndexOf(speaker);
                    }
                    string text = ritualTexts[speakCount % ritualTexts.Count];
                    speakCount++;
                    MoteMaker.ThrowText(speaker.DrawPos, speaker.Map, text, 7f);
                }
            }
            float progress = (float)TicksPassed / totalTicks;
            //进度条跟随晋升者头顶
            if (host != null && host.Spawned)
            {
                if (progressBar == null)
                    progressBar = EffecterDefOf.ProgressBar.Spawn();

                progressBar.EffectTick(host, TargetInfo.Invalid);

                var mote = ((SubEffecter_ProgressBar)progressBar.children[0]).mote;
                if (mote != null)
                {
                    mote.progress = progress;
                    mote.offsetZ = -0.5f;
                }
            }

            magicAreaEffect?.Update(progress);

            if (TicksPassed >= totalTicks)
            {
                progressBar?.Cleanup();
                progressBar = null;
                lord.ReceiveMemo(MemoSuccess);
            }
        }

        public override void UpdateAllDuties()
        {
            foreach (var pawn in lord.ownedPawns)
            {
                if (pawn == host)
                {
                    bool atPosition = host.Spawned && host.Position == hostStandCell;
                    pawn.mindState.duty = atPosition
                        ? new PawnDuty(DutyDefOf.WaitForRitualParticipants, hostStandCell, hostStandCell)
                        : new PawnDuty(GotoDef, hostStandCell);
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(SpectateCircleDef, spot)
                    {
                        spectateRect = centerRect,
                        spectateDistance = new IntRange(circleRadius, circleRadius + 2)
                    };
                }
                //原版要求：duty 改变后必须通知 job 系统立刻响应
                pawn.jobs?.CheckForJobOverride();
            }
        }
        public override ThinkTreeDutyHook VoluntaryJoinDutyHookFor(Pawn p)
            => ThinkTreeDutyHook.HighPriority;
    }

    // 阶段3：下一Tick解散Lord
    public class LordToil_RavenRite_Finish : LordToil
    {
        private bool pendingRemove;

        public override void Init() => pendingRemove = true;

        public override void LordToilTick()
        {
            if (pendingRemove)
                lord.Map.lordManager.RemoveLord(lord);
        }

        public override void UpdateAllDuties()
        {
            foreach (var pawn in lord.ownedPawns)
                pawn.mindState.duty = new PawnDuty(DutyDefOf.TravelOrWait);
        }
    }

    // 工厂：启动仪式
    public static class RavenRiteLordFactory
    {
        public static void StartRite(
            PromotionRitualSelection selection,
            Thing building,
            int durationTicks = 2500,
            string ritualLabel = "渡鸦仪式",
            int circleRadius = 4,
            Action<bool> onFinished = null)
        {
            Pawn host = selection.GetFirst("host");
            List<Pawn> participants = selection.Participants;
            Map map = building.Map;
            IntVec3 spot = building.Position;

            if (host == null || map == null) return;

            var allPawns = new List<Pawn> { host };
            allPawns.AddRange(participants.Where(p => p != host && !p.Dead));

            // 先从旧 Lord 移除，避免重复加入报错
            foreach (var pawn in allPawns)
                pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);

            var lordJob = new LordJob_RavenRite(
                altarSpot: spot,
                host: host,
                participants: participants,
                durationTicks: durationTicks,
                ritualLabel: ritualLabel,
                circleRadius: circleRadius,
                onFinished: onFinished);
            LordMaker.MakeNewLord(
                faction: Faction.OfPlayer,
                lordJob: lordJob,
                map: map,
                startingPawns: allPawns);
        }
    }
}