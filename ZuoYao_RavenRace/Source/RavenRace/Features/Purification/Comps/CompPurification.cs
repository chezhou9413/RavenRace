using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Features.Purification
{
    public class CompProperties_Purification : CompProperties
    {
        public CompProperties_Purification()
        {
            this.compClass = typeof(CompPurification);
        }
    }

    /// <summary>
    /// 渡鸦金乌纯化核心组件。
    /// 完全独立于杂交血脉系统，专注于金乌浓度的管理与飞升阶段突破。
    /// </summary>
    public class CompPurification : ThingComp
    {
        // ==========================================
        // 核心数据
        // ==========================================
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
            // 每次生成或读档时，确保持有对应阶段的加成
            RefreshPurificationBonuses();
        }

        // ==============================================================
        // 核心逻辑：浓度与阶段控制
        // ==============================================================

        /// <summary>
        /// 获取当前纯化阶段允许的最高浓度物理上限。
        /// </summary>
        public float GetMaxConcentrationLimit()
        {
            float limit = 1.0f; // 默认为满值
            var allStages = DefDatabase<PurificationStageDef>.AllDefsListForReading;
            if (allStages.NullOrEmpty()) return limit;

            var currentStageDef = allStages.FirstOrDefault(s => s.stageIndex == this.currentPurificationStage);
            if (currentStageDef != null)
            {
                limit = currentStageDef.concentrationThreshold;
            }
            return limit;
        }

        /// <summary>
        /// 尝试安全地增加金乌血脉浓度。
        /// 会同时受到“当前纯化阶段物理上限”和“来源物品极限”的双重约束。
        /// </summary>
        public void TryAddGoldenCrowConcentration(float amount, float sourceMaxLimit = 1.0f)
        {
            float stageLimit = GetMaxConcentrationLimit();
            float hardLimit = Mathf.Min(stageLimit, sourceMaxLimit);

            if (this.goldenCrowConcentration >= hardLimit)
            {
                return;
            }

            this.goldenCrowConcentration += amount;

            if (this.goldenCrowConcentration > hardLimit)
            {
                this.goldenCrowConcentration = hardLimit;
            }

            RefreshPurificationBonuses();
        }

        // ==============================================================
        // 奖励发放
        // ==============================================================
        public void RefreshPurificationBonuses()
        {
            if (this.Pawn == null || this.Pawn.health == null) return;

            var allStages = DefDatabase<PurificationStageDef>.AllDefsListForReading;
            if (allStages.NullOrEmpty()) return;

            foreach (var stageDef in allStages)
            {
                bool shouldHaveBonus = this.currentPurificationStage >= stageDef.stageIndex;

                // 1. Hediffs
                if (stageDef.grantedHediffs != null)
                {
                    foreach (var hDef in stageDef.grantedHediffs)
                    {
                        BloodlineUtility.ToggleHediff(this.Pawn, hDef, shouldHaveBonus);
                    }
                }

                // 2. Abilities
                if (stageDef.grantedAbilities != null)
                {
                    foreach (var aDef in stageDef.grantedAbilities)
                    {
                        BloodlineUtility.ToggleAbility(this.Pawn, aDef, shouldHaveBonus);
                    }
                }

                // 3. Traits (只增不减)
                if (shouldHaveBonus && stageDef.grantedTraits != null && this.Pawn.story != null && this.Pawn.story.traits != null)
                {
                    foreach (var tDef in stageDef.grantedTraits)
                    {
                        if (!this.Pawn.story.traits.HasTrait(tDef))
                        {
                            this.Pawn.story.traits.GainTrait(new Trait(tDef));
                        }
                    }
                }
            }

            Pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        public override string CompInspectStringExtra()
        {
            if (!RavenRaceMod.Settings.enableDebugMode) return null;
            return $"Purification Stage: {currentPurificationStage} (Limit: {GetMaxConcentrationLimit():P0})";
        }
    }
}