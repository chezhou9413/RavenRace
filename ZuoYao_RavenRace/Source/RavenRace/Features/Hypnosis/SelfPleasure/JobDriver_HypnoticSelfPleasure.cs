using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using RavenRace.Features.Hypnosis.Commands;

namespace RavenRace.Features.Hypnosis.SelfPleasure
{
    public class JobDriver_HypnoticSelfPleasure : JobDriver
    {
        private const int DurationTicks = 600;

        // 这里我们通过硬编码或者查找的方式获取对应的 CommandDef
        // 为了架构更优雅，以后可以把 CommandDef 传参进 Job，但现在为了兼容性，我们直接查找
        private HypnosisCommandDef CommandDef => DefDatabase<HypnosisCommandDef>.GetNamed("Raven_HypnosisCmd_SelfPleasure");

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. 开始时的准备：添加 Hediff，RimTalk
            Toil prepare = ToilMaker.MakeToil("Prepare");
            prepare.initAction = delegate
            {
                pawn.pather.StopDead();

                // [新增] 添加催眠状态 Hediff
                if (CommandDef.activeHediffDef != null)
                {
                    pawn.health.AddHediff(CommandDef.activeHediffDef);
                }

                // RimTalk
                RimTalkCompat.TryAddTalkRequest(pawn, "Mmm... body... moving on its own... ahh...");
            };
            prepare.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return prepare;

            // 2. 执行过程
            Toil pleasure = ToilMaker.MakeToil("Pleasure");
            pleasure.defaultCompleteMode = ToilCompleteMode.Delay;
            pleasure.defaultDuration = DurationTicks;
            pleasure.socialMode = RandomSocialMode.Off;

            pleasure.tickAction = delegate
            {
                // 维持 Hediff 时间 (防止因为 Duration 长于 Hediff 消失时间而失效)
                // 简单的做法是让 Hediff 时间够长，然后在结束时手动移除

                if (pawn.IsHashIntervalTick(60))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart, 0.6f);
                    if (Rand.Chance(0.3f)) MoteMaker.ThrowText(pawn.DrawPos + new Vector3(0, 0, 0.5f), pawn.Map, "♥", Color.magenta);
                }

                if (pawn.needs?.joy != null)
                {
                    pawn.needs.joy.GainJoy(0.002f, RavenDefOf.Raven_AdultEntertainment ?? JoyKindDefOf.Social);
                }
            };

            // [新增] 结束时的清理：移除 Hediff，添加 Thought
            pleasure.AddFinishAction(delegate
            {
                // 1. 移除状态 Hediff
                if (CommandDef.activeHediffDef != null)
                {
                    var h = pawn.health.hediffSet.GetFirstHediffOfDef(CommandDef.activeHediffDef);
                    if (h != null) pawn.health.RemoveHediff(h);
                }

                // 2. 添加结果 Thought (只有正常完成或被打断时才加，死亡不加)
                if (!pawn.Dead && pawn.needs?.mood != null && CommandDef.outcomeThoughtDef != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(CommandDef.outcomeThoughtDef);
                }
            });

            yield return pleasure;
        }
    }
}