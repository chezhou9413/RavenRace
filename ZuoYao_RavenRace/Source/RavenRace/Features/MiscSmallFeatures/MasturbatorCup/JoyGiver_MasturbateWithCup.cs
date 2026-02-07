using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.MasturbatorCup
{
    /// <summary>
    /// 自动从背包中使用飞机杯进行娱乐
    /// </summary>
    public class JoyGiver_MasturbateWithCup : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            // 检查背包中是否有飞机杯
            Thing cup = null;
            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    if (pawn.inventory.innerContainer[i].def == RavenDefOf.Raven_Item_MasturbatorCup)
                    {
                        cup = pawn.inventory.innerContainer[i];
                        break;
                    }
                }
            }

            if (cup == null) return null;

            // 1.6/1.5 特性：如果是在携带物品上运行，不需要 TargetA 必须在地上
            // 直接给 Job
            return JobMaker.MakeJob(def.jobDef, pawn); // TargetA 是自己
        }
    }
}