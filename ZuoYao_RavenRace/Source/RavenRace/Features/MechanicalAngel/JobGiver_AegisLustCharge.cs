using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MechanicalAngel
{
    /// <summary>
    /// 艾吉斯的专属充能 AI 节点。
    /// 继承自原版 JobGiver_GetEnergy，复用其阈值检测逻辑。
    /// 现已完美对接原版机械师系统。
    /// </summary>
    public class JobGiver_AegisLustCharge : JobGiver_GetEnergy
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            // 1. 检查是否需要充能（原版阈值判断）
            if (!this.ShouldAutoRecharge(pawn)) return null;

            // 2. 检查玩家是否开启了“主动榨取”开关
            var core = pawn.TryGetComp<CompAegisCore>();
            if (core == null || !core.allowLustCharge) return null;

            // 3. 【核心修改】通过原版扩展方法获取该机械体的绑定机械师主管！
            Pawn master = pawn.GetOverseer();

            // 确保主人在同地图、存活，且可以被到达
            if (master != null && master.Map == pawn.Map && !master.Dead && !master.IsForbidden(pawn))
            {
                if (pawn.CanReach(master, PathEndMode.Touch, Danger.Deadly))
                {
                    // 找到主人！返回榨取工作
                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Raven_Job_AegisLustCharge"), master);
                    return job;
                }
            }

            // 如果没有主人或主人无法到达，返回 null（节点失效，退回到原版的充电站逻辑）
            return null;
        }
    }
}