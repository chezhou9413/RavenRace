using RimWorld;
using Verse;
using Verse.AI;

namespace RavenRace.Features.MechanicalAngel
{
    /// <summary>
    /// 艾吉斯紧急救援主人的行为节点。
    /// 放在 ThinkTree 最顶端。只要主管倒地且不在床上，艾吉斯会放下一切强制执行救援。
    /// 现已完美对接原版机械师系统。
    /// </summary>
    public class JobGiver_AegisRescueMaster : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            // 如果艾吉斯自己倒了或无法移动，或者正在发情暴走，则无法救援
            if (pawn.Downed || !pawn.Awake() || pawn.GetComp<CompAegisCore>()?.isRampaging == true)
                return null;

            // 【核心修改】获取原版机械师主管
            Pawn master = pawn.GetOverseer();

            // 确保主人存活、在当前地图，且已经倒地，且尚未躺在床上
            if (master != null && master.Spawned && master.Map == pawn.Map && master.Downed && !master.InBed())
            {
                // 检查是否能够到达主人身边
                if (!pawn.CanReach(master, PathEndMode.OnCell, Danger.Deadly)) return null;

                // 寻找一个适合主人躺下的床铺 (使用原版医疗分配逻辑)
                Building_Bed bed = RestUtility.FindBedFor(master, pawn, false, false, master.GuestStatus);
                if (bed != null && master.CanReserve(bed, 1, -1, null, false))
                {
                    Job rescueJob = JobMaker.MakeJob(JobDefOf.Rescue, master, bed);
                    rescueJob.count = 1;
                    return rescueJob;
                }
            }

            return null;
        }
    }
}