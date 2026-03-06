using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.AI;
using Verse.AI.Group;

namespace RavenRace.Features.ReproductionRequest
{
    public class FloatMenuOptionProvider_ReproductionRequest : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptions(FloatMenuContext context)
        {
            if (context.IsMultiselect)
            {
                yield break;
            }

            Pawn negotiator = context.FirstSelectedPawn;
            if (negotiator == null || negotiator.Drafted)
            {
                yield break;
            }

            foreach (Thing t in context.ClickedThings)
            {
                if (t is Pawn targetPawn && targetPawn.GetLord()?.LordJob is LordJob_ReproductionRequest lordJob)
                {
                    if (lordJob.leader == targetPawn && lordJob.isWaitingForDialog)
                    {
                        yield return new FloatMenuOption("与扶桑领队交涉", () =>
                        {
                            Job job = JobMaker.MakeJob(ReproductionRequestDefOf.Raven_Job_NegotiateWithLeader, targetPawn);
                            negotiator.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        });
                    }
                }
            }
        }

        // [修复] 将 `private` 修改为 `public`，使其能被 JobDriver 调用
        public static void OpenReproductionDialog(Pawn negotiator, LordJob_ReproductionRequest lordJob)
        {
            string text = "尊敬的殖民地领袖。我们是扶桑的巡游播种小队。队伍里姐妹们的发情期到了，极其渴望高质量的基因。\n\n我们注意到贵殖民地有健康的男性，能否将他借给我们一段时间？我们保证不会弄坏他，只是会借用他那喷薄的生命精华。\n\n作为回报，扶桑会赠予你们一份极其珍贵的礼物——一只纯正的渡鸦大统领。\n\n您意下如何？";
            DiaNode node = new DiaNode(text);

            DiaOption optAccept = new DiaOption("没问题，这是为了种族的延续（选择一名男性交出）");
            optAccept.action = () =>
            {
                TargetingParameters tp = new TargetingParameters
                {
                    canTargetPawns = true,
                    validator = (TargetInfo target) =>
                    {
                        if (target.Thing is Pawn p)
                        {
                            return p.IsColonist && p.gender == Gender.Male && !p.Downed && !p.Dead;
                        }
                        return false;
                    }
                };
                Find.Targeter.BeginTargeting(tp, (LocalTargetInfo target) =>
                {
                    lordJob.AcceptAndStartQueue(target.Pawn);
                }, negotiator);
            };
            optAccept.resolveTree = true;
            node.options.Add(optAccept);

            DiaOption optReject = new DiaOption("这这不能...还是算了吧");
            optReject.action = () =>
            {
                lordJob.RejectRequest();
            };
            optReject.resolveTree = true;
            node.options.Add(optReject);

            Find.WindowStack.Add(new Dialog_NodeTree(node, true, false, null));
        }
    }
}