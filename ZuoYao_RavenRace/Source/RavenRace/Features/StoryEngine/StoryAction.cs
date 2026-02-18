using Verse;
using RimWorld;
using RavenRace.Features.Operator;

namespace RavenRace.Features.StoryEngine
{
    /// <summary>
    /// 剧情动作的抽象基类。
    /// </summary>
    public abstract class StoryAction
    {
        public abstract void Execute();
    }

    // ================= 具体实现 =================

    /// <summary>
    /// 设置一个全局 Flag。
    /// </summary>
    public class StoryAction_SetFlag : StoryAction
    {
        public string flagKey;
        public bool value = true;

        public override void Execute()
        {
            StoryWorldComponent.SetFlag(flagKey, value);
        }
    }

    /// <summary>
    /// 改变左爻的好感度。
    /// </summary>
    public class StoryAction_ChangeFavorability : StoryAction
    {
        public int amount;

        public override void Execute()
        {
            WorldComponent_OperatorManager.ChangeFavorability(amount);
            // 可以在这里抛出 Mote 文字
        }
    }

    /// <summary>
    /// 给予物品。
    /// </summary>
    public class StoryAction_GiveItem : StoryAction
    {
        public ThingDef thingDef;
        public int count = 1;

        public override void Execute()
        {
            if (thingDef == null) return;
            Thing t = ThingMaker.MakeThing(thingDef);
            t.stackCount = count;

            // 尝试放到交易空投点或玩家脚下
            IntVec3 dropPos = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            DropPodUtility.DropThingsNear(dropPos, Find.CurrentMap, new System.Collections.Generic.List<Thing> { t });

            Messages.Message($"获得了 {t.LabelCap} x{count}", MessageTypeDefOf.PositiveEvent);
        }
    }

    /// <summary>
    /// 触发一个事件（Incident）。
    /// </summary>
    public class StoryAction_TriggerIncident : StoryAction
    {
        public IncidentDef incidentDef;

        public override void Execute()
        {
            if (incidentDef == null) return;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, Find.CurrentMap);
            parms.forced = true;

            if (incidentDef.Worker.TryExecute(parms))
            {
                Messages.Message($"事件 {incidentDef.label} 已触发。", MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}