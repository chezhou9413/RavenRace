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
        public bool Active => true; // 只要添加了组件就生效，不分性别

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref fullness, "ravenMilkFullness", 0f);
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = this.parent as Pawn;
            if (pawn == null || pawn.Dead) return;

            float hungerFactor = 1f;
            if (pawn.needs?.food != null)
            {
                if (pawn.needs.food.Starving) hungerFactor = 0f;
            }

            float increase = (1f / (Props.milkIntervalDays * 60000f)) * hungerFactor;

            fullness += increase;
            if (fullness > 1f) fullness = 1f;
        }

        public override string CompInspectStringExtra()
        {
            if (!Active) return null;
            return Props.displayStringKey.Translate() + ": " + fullness.ToStringPercent();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (fullness >= 0.95f)
            {
                yield return new Command_Action
                {
                    defaultLabel = "RavenRace_GatherMilk".Translate(),
                    defaultDesc = "RavenRace_GatherMilkDesc".Translate(),
                    icon = Props.milkDef?.uiIcon ?? BaseContent.BadTex,
                    action = () =>
                    {
                        GatherMilk(parent as Pawn);
                    }
                };
            }
        }

        public void GatherMilk(Pawn doer)
        {
            if (!Active || fullness < 0.01f) return;

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