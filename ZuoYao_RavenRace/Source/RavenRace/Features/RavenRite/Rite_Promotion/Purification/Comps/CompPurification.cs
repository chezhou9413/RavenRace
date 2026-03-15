using System;
using System.Collections.Generic;
using System.Linq;
using RavenRace.Features.RavenRite.Rite_Promotion.Purification.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Features.RavenRite.Rite_Promotion.Purification.Comps
{
    public class CompProperties_Purification : CompProperties
    {
        public CompProperties_Purification()
        {
            this.compClass = typeof(CompPurification);
        }
    }

    public class CompPurification : ThingComp
    {
        private float goldenCrowConcentration = 0f;
        public int currentPurificationStage = 0;

        public Pawn Pawn => (Pawn)this.parent;

        public float GoldenCrowConcentration
        {
            get => goldenCrowConcentration;
            set => goldenCrowConcentration = Mathf.Clamp01(value);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref goldenCrowConcentration, "goldenCrowConcentration", 0f);
            Scribe_Values.Look(ref currentPurificationStage, "currentPurificationStage", 0);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RefreshPurificationBonuses();
        }

        public float GetMaxConcentrationLimit()
        {
            var allStages = DefDatabase<PurificationStageDef>.AllDefsListForReading;
            var currentStageDef = allStages.FirstOrDefault(s => s.stageIndex == this.currentPurificationStage);
            return currentStageDef?.concentrationThreshold ?? 1.0f;
        }

        public void TryAddGoldenCrowConcentration(float amount, float sourceMaxLimit = 1.0f)
        {
            float stageLimit = GetMaxConcentrationLimit();
            float hardLimit = Mathf.Min(stageLimit, sourceMaxLimit);
            if (this.goldenCrowConcentration >= hardLimit) return;

            this.goldenCrowConcentration = Mathf.Min(this.goldenCrowConcentration + amount, hardLimit);
            RefreshPurificationBonuses();
        }

        /// <summary>
        /// 核心刷新逻辑
        /// Hediff: 覆盖制（仅保留最高级）
        /// Ability/Trait: 解锁制（累加继承）
        /// </summary>
        public void RefreshPurificationBonuses()
        {
            if (this.Pawn == null || this.Pawn.health == null) return;

            var allStages = DefDatabase<PurificationStageDef>.AllDefsListForReading.OrderBy(s => s.stageIndex).ToList();
            if (allStages.NullOrEmpty()) return;

            // 1. 处理 Hediff (覆盖制)
            // 先收集所有定义过的阶段 Hediff，全部移除
            foreach (var stage in allStages)
            {
                if (stage.grantedHediffs != null)
                {
                    foreach (var hDef in stage.grantedHediffs)
                    {
                        var existing = this.Pawn.health.hediffSet.GetFirstHediffOfDef(hDef);
                        if (existing != null) this.Pawn.health.RemoveHediff(existing);
                    }
                }
            }

            // 寻找当前拥有的最高级且合法的阶段，添加其 Hediff
            var currentStageDef = allStages.LastOrDefault(s => s.stageIndex <= this.currentPurificationStage);
            if (currentStageDef?.grantedHediffs != null)
            {
                foreach (var hDef in currentStageDef.grantedHediffs)
                {
                    this.Pawn.health.AddHediff(hDef);
                }
            }

            // 2. 处理技能和特性 (解锁继承制)
            foreach (var stageDef in allStages)
            {
                bool isUnlocked = this.currentPurificationStage >= stageDef.stageIndex;

                if (stageDef.grantedAbilities != null)
                {
                    foreach (var aDef in stageDef.grantedAbilities)
                    {
                        BloodlineUtility.ToggleAbility(this.Pawn, aDef, isUnlocked);
                    }
                }

                if (isUnlocked && stageDef.grantedTraits != null && this.Pawn.story?.traits != null)
                {
                    foreach (var tDef in stageDef.grantedTraits)
                    {
                        if (!this.Pawn.story.traits.HasTrait(tDef))
                            this.Pawn.story.traits.GainTrait(new Trait(tDef));
                    }
                }
            }

            Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}