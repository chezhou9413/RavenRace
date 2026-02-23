using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline; // 必须引用血脉命名空间

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

        /// <summary>
        /// 核心拦截：只有当Mod开启、设置允许，且角色真正拥有血脉时才执行逻辑
        /// </summary>
        private bool IsActiveAndValid(Pawn pawn)
        {
            if (!RavenRaceMod.Settings.enableMinchoCompat) return false;
            if (!MinchoCompatUtility.IsMinchoActive) return false;
            if (pawn == null || pawn.Dead) return false;

            var comp = pawn.TryGetComp<CompBloodline>();
            return MinchoCompatUtility.HasMinchoBloodline(comp);
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = this.parent as Pawn;

            // 拦截检查
            if (!IsActiveAndValid(pawn)) return;

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
                Produce(pawn);
                progress = 0f;
            }
        }

        private void Produce(Pawn pawn)
        {
            if (pawn.Map == null) return; // 不在地图上不生成(但在世界地图会累积进度，回到地图瞬间掉落)

            if (Props.resourceDef == null) return; // 安全检查

            // 生成物品
            Thing thing = ThingMaker.MakeThing(Props.resourceDef);
            thing.stackCount = Props.amount;

            // 尝试放置在脚下
            if (GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, out Thing resultingThing))
            {
                Messages.Message("RavenRace_Message_MinchoProduced".Translate(pawn.LabelShort, thing.Label), new LookTargets(pawn, resultingThing), MessageTypeDefOf.PositiveEvent);
            }
        }

        // [核心] 这里决定了左下角信息栏的显示
        public override string CompInspectStringExtra()
        {
            Pawn pawn = this.parent as Pawn;

            // 拦截检查，无血脉不显示UI
            if (!IsActiveAndValid(pawn)) return null;

            if (Props.resourceDef == null) return null;
            return Props.labelKey.Translate() + ": " + progress.ToStringPercent();
        }
    }
}