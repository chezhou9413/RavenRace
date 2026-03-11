using RavenRace.Features.RavenRite.CustomRiteCore.Pojo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RavenRace.Features.RavenRite.CustomRiteCore.RiteWoker
{
    public abstract class RavenRiteWorker
    {
        //携带玩家的完整选择结果和所在建筑
        public abstract void Execute(PromotionRitualSelection selection, Thing building);

        //被禁用时追加说明，返回 null 则不显示。
        public virtual string DisabledReason(Thing building) => null;
    }
}
