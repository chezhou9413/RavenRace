using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Compat.MuGirl
{
    public class CompProperties_RavenMilkable : CompProperties
    {
        public ThingDef milkDef;
        public int milkAmount = 11;
        public float milkIntervalDays = 1f;
        public string displayStringKey = "RavenRace_MilkFullness";

        // 新增：XML 驱动的血脉条件
        public string requireBloodline;
        public string requireBloodline2;
        public string forbidBloodline;

        public CompProperties_RavenMilkable()
        {
            this.compClass = typeof(CompRavenMilkable);
        }
    }

    public class CompRavenMilkable : ThingComp
    {
        public CompProperties_RavenMilkable Props => (CompProperties_RavenMilkable)this.props;

        private float fullness = 0f;
        public float Fullness => fullness;

        // 核心：动态评估是否满足XML中定义的血脉条件
        public bool Active
        {
            get
            {
                Pawn pawn = this.parent as Pawn;
                if (pawn == null || pawn.Dead) return false;

                var bloodlineComp = pawn.TryGetComp<Features.Bloodline.CompBloodline>();
                if (bloodlineComp == null || bloodlineComp.BloodlineComposition == null) return false;

                var dict = bloodlineComp.BloodlineComposition;

                // 检查条件1
                if (!string.IsNullOrEmpty(Props.requireBloodline) &&
                    (!dict.ContainsKey(Props.requireBloodline) || dict[Props.requireBloodline] <= 0f))
                    return false;

                // 检查条件2 (用于Combo)
                if (!string.IsNullOrEmpty(Props.requireBloodline2) &&
                    (!dict.ContainsKey(Props.requireBloodline2) || dict[Props.requireBloodline2] <= 0f))
                    return false;

                // 检查排斥条件
                if (!string.IsNullOrEmpty(Props.forbidBloodline) &&
                    dict.ContainsKey(Props.forbidBloodline) && dict[Props.forbidBloodline] > 0f)
                    return false;

                return true;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            // 为了防止多个同类组件互相覆盖数据，保存时加上物品的DefName作为后缀区分
            string saveKey = "ravenMilkFullness_" + (Props.milkDef?.defName ?? "unknown");
            Scribe_Values.Look(ref fullness, saveKey, 0f);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!Active) return;

            Pawn pawn = this.parent as Pawn;
            float hungerFactor = 1f;
            if (pawn.needs?.food != null && pawn.needs.food.Starving)
            {
                hungerFactor = 0f;
            }

            float increase = (1f / (Props.milkIntervalDays * 60000f)) * hungerFactor;
            fullness += increase;
            if (fullness > 1f) fullness = 1f;
        }

        public override string CompInspectStringExtra()
        {
            if (!Active || Props.milkDef == null) return null;
            return Props.displayStringKey.Translate() + ": " + fullness.ToStringPercent();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Active && fullness >= 0.95f && Props.milkDef != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RavenRace_GatherMilk".Translate(),
                    defaultDesc = "RavenRace_GatherMilkDesc".Translate(),
                    icon = Props.milkDef.uiIcon ?? BaseContent.BadTex,
                    action = () => GatherMilk(parent as Pawn)
                };
            }
        }

        public void GatherMilk(Pawn doer)
        {
            if (!Active || fullness < 0.01f || Props.milkDef == null) return;

            int amount = GenMath.RoundRandom(Props.milkAmount * fullness);
            if (amount > 0)
            {
                Thing milk = ThingMaker.MakeThing(Props.milkDef);
                milk.stackCount = amount;
                GenPlace.TryPlaceThing(milk, doer.Position, doer.Map, ThingPlaceMode.Near);

                Messages.Message("RavenRace_Msg_MilkGathered".Translate(doer.LabelShort, amount, milk.Label), doer, MessageTypeDefOf.PositiveEvent);
            }

            fullness = 0f;
        }
    }
}