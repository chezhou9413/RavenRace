using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RavenRace.Features.ReproductionRequest
{
    [StaticConstructorOnStartup]
    public class LordJob_ReproductionRequest : LordJob
    {
        private static readonly Material QuestionMarkMat = MaterialPool.MatFrom("UI/Overlays/QuestionMark", ShaderDatabase.MetaOverlay);

        public Pawn leader;
        public IntVec3 chillSpot;
        public Pawn selectedMale;
        public List<Pawn> lovinQueue = new List<Pawn>();
        public bool isWaitingForDialog = true;
        public bool isProcessingQueue = false;

        public LordJob_ReproductionRequest() { }
        public LordJob_ReproductionRequest(Pawn leader, IntVec3 chillSpot) { this.leader = leader; this.chillSpot = chillSpot; }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            LordToil_WaitRequest waitToil = new LordToil_WaitRequest(chillSpot);
            graph.StartingToil = waitToil;
            LordToil_QueueLovin queueToil = new LordToil_QueueLovin(this);
            graph.AddToil(queueToil);
            LordToil_ExitMap exitToil = new LordToil_ExitMap(LocomotionUrgency.Jog, false, false);
            graph.AddToil(exitToil);
            Transition toQueue = new Transition(waitToil, queueToil);
            toQueue.AddTrigger(new Trigger_Memo("RequestAccepted"));
            graph.AddTransition(toQueue);
            Transition waitToExit = new Transition(waitToil, exitToil);
            waitToExit.AddTrigger(new Trigger_Memo("RequestRejected"));
            waitToExit.AddTrigger(new Trigger_TicksPassed(60000));
            waitToExit.AddPreAction(new TransitionAction_Message("扶桑的女孩们因为久久等不到回应，失望地离开了。", null, 1f));
            graph.AddTransition(waitToExit);
            Transition queueToExitSuccess = new Transition(queueToil, exitToil);
            queueToExitSuccess.AddTrigger(new Trigger_Memo("AllFinished"));
            queueToExitSuccess.AddPreAction(new TransitionAction_Custom(() => DropRewardAndThank()));
            graph.AddTransition(queueToExitSuccess);
            Transition queueToExitFail = new Transition(queueToil, exitToil);
            queueToExitFail.AddTrigger(new Trigger_Memo("TargetInvalid"));
            queueToExitFail.AddPreAction(new TransitionAction_Message("由于男性目标发生意外或离开，扶桑的女孩们不得不终止了狂欢，失望地离开了。", null, 1f));
            graph.AddTransition(queueToExitFail);
            Transition hurtExit = new Transition(waitToil, exitToil);
            hurtExit.AddSources(new LordToil[] { queueToil });
            hurtExit.AddTrigger(new Trigger_PawnHarmed());
            hurtExit.AddPreAction(new TransitionAction_Message("扶桑的使者受到了攻击！任务取消！", MessageTypeDefOf.NegativeEvent));
            graph.AddTransition(hurtExit);
            return graph;
        }

        public override void LordJobTick()
        {
            base.LordJobTick();
            if (isWaitingForDialog && leader != null && leader.Spawned && !leader.Dead)
            {
                // [修复] 使用 GenDraw.DrawMeshNowOrLater 手动构建绘制逻辑
                Vector3 drawPos = leader.DrawPos;
                drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                drawPos.z += 0.7f; // 向上微调

                // 创建变换矩阵
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(0.6f, 1f, 0.6f)); // 调整缩放

                // 使用正确的绘制方法
                GenDraw.DrawMeshNowOrLater(MeshPool.plane10, matrix, QuestionMarkMat, true);
            }

            if (isProcessingQueue && (selectedMale == null || selectedMale.Dead || !selectedMale.Spawned))
            {
                lord.ReceiveMemo("TargetInvalid");
                isProcessingQueue = false;
            }
        }

        public void AcceptAndStartQueue(Pawn maleTarget) { this.selectedMale = maleTarget; this.isWaitingForDialog = false; this.isProcessingQueue = true; this.lovinQueue = new List<Pawn>(lord.ownedPawns); lord.ReceiveMemo("RequestAccepted"); }
        public void RejectRequest() { this.isWaitingForDialog = false; lord.ReceiveMemo("RequestRejected"); }
        public void Notify_FemaleFinishedLovin(Pawn female) { if (lovinQueue.Contains(female)) { lovinQueue.Remove(female); } if (lovinQueue.Count == 0) { lord.ReceiveMemo("AllFinished"); } else { lord.CurLordToil.UpdateAllDuties(); } }
        private void DropRewardAndThank() { if (selectedMale != null && selectedMale.Spawned && !selectedMale.Dead) { selectedMale.needs?.mood?.thoughts?.memories?.TryGainMemory(ReproductionRequestDefOf.Raven_Thought_SqueezedByGroup); HealthUtility.AdjustSeverity(selectedMale, ReproductionRequestDefOf.Raven_Hediff_SqueezedDry, 1.0f); } Pawn archon = PawnGenerator.GeneratePawn(new PawnGenerationRequest(RavenDefOf.Raven_HighArchon, Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, true)); IntVec3 dropSpot = DropCellFinder.TradeDropSpot(Map); DropPodUtility.DropThingsNear(dropSpot, Map, new List<Thing> { archon }, 110, false, false, true, false, true, Faction.OfPlayer); Find.LetterStack.ReceiveLetter("狂欢结束", "女孩们心满意足地抹了抹嘴角的痕迹，感谢了你们的无私奉献。作为约定，她们留下了一只珍贵的渡鸦大统领。", LetterDefOf.PositiveEvent, new LookTargets(archon)); }
        public override void ExposeData() { Scribe_References.Look(ref leader, "leader"); Scribe_References.Look(ref selectedMale, "selectedMale"); Scribe_Values.Look(ref chillSpot, "chillSpot"); Scribe_Values.Look(ref isWaitingForDialog, "isWaitingForDialog", true); Scribe_Values.Look(ref isProcessingQueue, "isProcessingQueue", false); Scribe_Collections.Look(ref lovinQueue, "lovinQueue", LookMode.Reference); if (Scribe.mode == LoadSaveMode.PostLoadInit && lovinQueue == null) { lovinQueue = new List<Pawn>(); } }
    }

    public class LordToil_WaitRequest : LordToil { private IntVec3 chillSpot; public LordToil_WaitRequest(IntVec3 spot) { this.chillSpot = spot; } public override void UpdateAllDuties() { for (int i = 0; i < lord.ownedPawns.Count; i++) { lord.ownedPawns[i].mindState.duty = new PawnDuty(ReproductionRequestDefOf.Raven_Duty_ReproductionRequest_Wait, chillSpot); } } }
    public class LordToil_QueueLovin : LordToil { private LordJob_ReproductionRequest myLordJob; public LordToil_QueueLovin(LordJob_ReproductionRequest job) { this.myLordJob = job; } public override void UpdateAllDuties() { if (myLordJob.lovinQueue.Count == 0 || myLordJob.selectedMale == null) return; Pawn activeFemale = myLordJob.lovinQueue[0]; for (int i = 0; i < lord.ownedPawns.Count; i++) { Pawn p = lord.ownedPawns[i]; if (p == activeFemale) { p.mindState.duty = new PawnDuty(ReproductionRequestDefOf.Raven_Duty_ReproductionRequest_Lovin, myLordJob.selectedMale); } else { p.mindState.duty = new PawnDuty(ReproductionRequestDefOf.Raven_Duty_ReproductionRequest_Wait, myLordJob.selectedMale.Position); } } } }
    public class JobGiver_RequestLovin : ThinkNode_JobGiver { protected override Job TryGiveJob(Pawn pawn) { Pawn target = pawn.mindState.duty.focus.Pawn; if (target == null || target.Dead || !pawn.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly)) { return null; } return JobMaker.MakeJob(ReproductionRequestDefOf.Raven_Job_ReproductionRequestLovin, target); } }
}