using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Compat.Mincho
{
    public class CompProperties_MinchoPassiveProduction : CompProperties
    {
        public ThingDef resourceDef; // 产出的物品
        public int amount = 10;      // 产出数量
        public float intervalDays = 1f; // 生产周期(天)
        public string labelKey = "RavenRace_MinchoChocolateFullness"; // UI显示的Key

        public CompProperties_MinchoPassiveProduction()
        {
            this.compClass = typeof(CompMinchoPassiveProduction);
        }
    }

    public class CompMinchoPassiveProduction : ThingComp
    {
        public CompProperties_MinchoPassiveProduction Props => (CompProperties_MinchoPassiveProduction)this.props;

        private float progress = 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref progress, "minchoProductionProgress", 0f);
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = this.parent as Pawn;
            if (pawn == null || pawn.Dead) return;

            // 饥饿时停止生产
            float hungerFactor = 1f;
            if (pawn.needs?.food != null && pawn.needs.food.Starving)
            {
                hungerFactor = 0f;
            }

            // 增加进度
            float increase = (1f / (Props.intervalDays * 60000f)) * hungerFactor;
            progress += increase;

            // 满了自动掉落
            if (progress >= 1f)
            {
                Produce();
                progress = 0f;
            }
        }

        private void Produce()
        {
            Pawn pawn = (Pawn)parent;
            if (pawn.Map == null) return; // 不在地图上不生成(但在世界地图会累积进度，回到地图瞬间掉落)

            // 生成物品
            Thing thing = ThingMaker.MakeThing(Props.resourceDef);
            thing.stackCount = Props.amount;

            // 尝试放置在脚下
            if (GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, out Thing resultingThing))
            {
                // 播放音效和消息
                // 复用产卵的声音或标准的物品掉落声
                // (DefDatabase<SoundDef>.GetNamedSilentFail("Hive_Spawn") ?? SoundDefOf.Standard_Drop).PlayOneShot(pawn);

                Messages.Message("RavenRace_Message_MinchoProduced".Translate(pawn.LabelShort, thing.Label), new LookTargets(pawn, resultingThing), MessageTypeDefOf.PositiveEvent);
            }
        }

        // [核心] 这里决定了左下角信息栏的显示
        public override string CompInspectStringExtra()
        {
            if (Props.resourceDef == null) return null;
            return Props.labelKey.Translate() + ": " + progress.ToStringPercent();
        }
    }
}