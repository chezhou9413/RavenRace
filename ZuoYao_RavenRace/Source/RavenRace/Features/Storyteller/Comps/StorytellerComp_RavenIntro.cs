using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Features.Storyteller.Comps
{
    public class StorytellerCompProperties_RavenIntro : StorytellerCompProperties
    {
        public IncidentDef incident;
        public int delayTicks = 2500; // 默认 1 小时

        public StorytellerCompProperties_RavenIntro()
        {
            this.compClass = typeof(StorytellerComp_RavenIntro);
        }
    }

    public class StorytellerComp_RavenIntro : StorytellerComp
    {
        private StorytellerCompProperties_RavenIntro Props => (StorytellerCompProperties_RavenIntro)props;

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            // 1. 基础检查：只对玩家主地图生效
            // StorytellerTick 是对所有 target 调用的（包括 World, Map, Caravan）
            // 我们只希望在这个事件在“地图”上触发
            Map map = target as Map;
            if (map == null || !map.IsPlayerHome) yield break;

            // 2. 检查是否已经触发过
            // 使用原版的 StoryState 记录，这是跨存档安全的标准做法
            if (target.StoryState.lastFireTicks.ContainsKey(Props.incident))
            {
                yield break;
            }

            // 3. 检查时间是否到达
            // 逻辑：当前游戏时间 >= 设定延迟
            // 这确保了即使中途切换叙事者（此时 TicksGame 很大），只要没触发过，就会立即触发
            if (Find.TickManager.TicksGame >= Props.delayTicks)
            {
                // 4. 检查事件是否可以触发
                // 生成默认参数进行检查 (Target = map)
                IncidentParms parms = GenerateParms(Props.incident.category, target);

                if (Props.incident.Worker.CanFireNow(parms))
                {
                    // 5. 返回 FiringIncident
                    // Storyteller 会处理后续的执行和记录 lastFireTicks
                    yield return new FiringIncident(Props.incident, this, parms);
                }
            }
        }
    }
}