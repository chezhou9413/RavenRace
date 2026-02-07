using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using System;

namespace RavenRace.Features.Reproduction
{
    public class HediffRavenPregnancy : HediffWithParents
    {
        public new GeneSet geneSet;
        public bool isRavenDescendant;

        public float GestationProgress => this.Severity;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref geneSet, "geneSet");
            Scribe_Values.Look(ref isRavenDescendant, "isRavenDescendant");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.Father != null && this.Father == this.pawn)
                {
                    Log.Warning($"[RavenRace]检测到灵卵孕育的父亲被设置为孕育者自身({pawn.Name})。这是一个危险的循环引用，已自动修正为无父亲。");
                    this.SetParents(this.pawn, null, this.geneSet);
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            float days = Math.Max(0.1f, RavenRaceMod.Settings.baseHatchingDays);
            float progressPerTick = 1f / (days * 60000f);
            this.Severity += progressPerTick;
            if (this.Severity >= 1f)
            {
                DoBirth();
            }
        }

        public void Initialize(Pawn fatherPawn, GeneSet genes, bool forceRaven)
        {
            base.SetParents(this.pawn, fatherPawn, genes);
            this.geneSet = genes;
            this.isRavenDescendant = forceRaven;
        }

        private void DoBirth()
        {
            bool isCarrierRaven = pawn.def == RavenDefOf.Raven_Race;
            bool isFatherRaven = Father != null && Father.def == RavenDefOf.Raven_Race;

            if (!isCarrierRaven && !isFatherRaven)
            {
                pawn.health.RemoveHediff(this);
                return;
            }

            if (this.Father != null && this.Father == this.pawn)
            {
                pawn.health.RemoveHediff(this);
                return;
            }

            Thing eggThing = ThingMaker.MakeThing(RavenDefOf.Raven_SpiritEgg);
            CompSpiritEgg eggComp = eggThing.TryGetComp<CompSpiritEgg>();

            if (eggComp != null)
            {
                eggComp.Initialize(this.pawn, this.Father, this.geneSet);
            }

            GenSpawn.Spawn(eggThing, this.pawn.Position, this.pawn.Map);

            if (this.pawn.gender == Gender.Female)
            {
                this.pawn.health.AddHediff(HediffDefOf.Lactating);
                this.pawn.health.AddHediff(HediffDefOf.PostpartumExhaustion);
            }
            else
            {
                HediffDef pain = DefDatabase<HediffDef>.GetNamedSilentFail("PainShock") ?? HediffDefOf.Anesthetic;
                this.pawn.health.AddHediff(pain);
            }

            Find.LetterStack.ReceiveLetter("RavenRace_LetterLabel_EggLaid".Translate(),
                "RavenRace_LetterText_EggLaid".Translate(this.pawn.LabelShort, (this.Father != null ? this.Father.LabelShort : "Unknown")),
                LetterDefOf.PositiveEvent, eggThing);

            this.pawn.health.RemoveHediff(this);
        }

        public override string LabelInBrackets => this.CurStage != null && !this.CurStage.label.NullOrEmpty() ? this.CurStage.label : base.LabelInBrackets;

        public override string TipStringExtra
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(base.TipStringExtra);
                sb.AppendLine("IncubationProgress".Translate() + ": " + this.Severity.ToStringPercent());
                float daysTotal = Math.Max(0.1f, RavenRaceMod.Settings.baseHatchingDays);
                float daysLeft = (1f - this.Severity) * daysTotal;
                int ticksLeft = (int)(daysLeft * 60000f);
                if (ticksLeft > 0)
                {
                    sb.AppendLine("TimeLeft".Translate() + ": " + ticksLeft.ToStringTicksToPeriod());
                }
                return sb.ToString();
            }
        }
    }
}