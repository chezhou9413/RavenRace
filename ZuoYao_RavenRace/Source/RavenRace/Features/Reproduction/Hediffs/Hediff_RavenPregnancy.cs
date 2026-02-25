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

        // [新增] 用于存储非生物实体（如墙）带来的特殊血脉标识
        public string customBloodline;

        public float GestationProgress => this.Severity;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref geneSet, "geneSet");
            Scribe_Values.Look(ref isRavenDescendant, "isRavenDescendant");
            Scribe_Values.Look(ref customBloodline, "customBloodline"); // 保存字段

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

        // [修改] 增加 customBloodline 参数，默认为 null
        public void Initialize(Pawn fatherPawn, GeneSet genes, bool forceRaven, string customBloodline = null)
        {
            base.SetParents(this.pawn, fatherPawn, genes);
            this.geneSet = genes;
            this.isRavenDescendant = forceRaven;
            this.customBloodline = customBloodline;
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
                // [传递] 将 customBloodline 一并传给灵卵组件
                eggComp.Initialize(this.pawn, this.Father, this.geneSet, this.customBloodline);
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

            // 处理提示文本：如果是墙体（customBloodline非空且没有实体父亲），显示神秘文本
            string fatherLabel = "Unknown";
            if (!string.IsNullOrEmpty(this.customBloodline) && this.Father == null)
            {
                fatherLabel = "某种坚硬的建筑结构";
            }
            else if (this.Father != null)
            {
                fatherLabel = this.Father.LabelShort;
            }

            Find.LetterStack.ReceiveLetter("RavenRace_LetterLabel_EggLaid".Translate(),
                "RavenRace_LetterText_EggLaid".Translate(this.pawn.LabelShort, fatherLabel),
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