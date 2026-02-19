using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Compat.Mincho
{
    /// <summary>
    /// HediffComp的属性定义类，用于在XML中配置生产参数。
    /// </summary>
    public class HediffCompProperties_MinchoProduction : HediffCompProperties
    {
        public ThingDef thingToProduce;
        public int productionIntervalTicks = 30000; // 默认为半天
        public int amountToProduce = 10;

        public HediffCompProperties_MinchoProduction()
        {
            this.compClass = typeof(HediffComp_MinchoProduction);
        }
    }

    /// <summary>
    /// 附加在珉巧血脉Hediff上的组件，实现自动生产薄荷巧克力。
    /// </summary>
    public class HediffComp_MinchoProduction : HediffComp
    {
        private int ticksToProduce;

        public HediffCompProperties_MinchoProduction Props => (HediffCompProperties_MinchoProduction)this.props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            // 首次添加时，重置计时器
            this.ticksToProduce = Props.productionIntervalTicks;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref this.ticksToProduce, "ticksToProduce", 0);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // 每帧减少计时
            this.ticksToProduce--;

            if (this.ticksToProduce <= 0)
            {
                // 计时结束，执行生产
                Produce();
                // 重置计时器
                this.ticksToProduce = Props.productionIntervalTicks;
            }
        }

        /// <summary>
        /// 生产物品并掉落在Pawn脚下。
        /// </summary>
        private void Produce()
        {
            // 确保Pawn存在且在地图上
            if (this.Pawn == null || !this.Pawn.Spawned || this.Pawn.Map == null)
            {
                return;
            }

            // 确保要生产的物品已定义
            if (Props.thingToProduce == null)
            {
                Log.ErrorOnce("[RavenRace] HediffComp_MinchoProduction: thingToProduce is not defined in XML!", 918273645);
                return;
            }

            // 创建物品
            Thing thing = ThingMaker.MakeThing(Props.thingToProduce);
            thing.stackCount = Props.amountToProduce;

            // 尝试将物品放置在Pawn附近
            if (GenPlace.TryPlaceThing(thing, this.Pawn.Position, this.Pawn.Map, ThingPlaceMode.Near, out Thing resultingThing))
            {
                // 发送消息通知玩家
                Messages.Message("RavenRace_Message_MinchoProduced".Translate(this.Pawn.LabelShort, thing.Label), this.Pawn, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}