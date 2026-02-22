using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace
{
    /// <summary>
    /// BloodlineUtility - 血脉系统核心工具类
    /// 负责提供血脉判定、Hediff 和技能的通用分发方法，消除冗余代码。
    /// </summary>
    public static class BloodlineUtility
    {
        /// <summary>
        /// 【通用判定】检查血脉组件中是否包含指定的一个或多个种族 DefName（且浓度 > 0）
        /// 支持传入多个别名以处理模组作者改名的情况 (如 "Milira" 和 "Milira_Race")
        /// </summary>
        public static bool HasBloodline(CompBloodline comp, params string[] raceDefNames)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;

            foreach (string defName in raceDefNames)
            {
                if (comp.BloodlineComposition.TryGetValue(defName, out float val) && val > 0f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 【通用状态分发】安全地为 Pawn 添加或移除指定的 Hediff
        /// </summary>
        public static void ToggleHediff(Pawn pawn, HediffDef hediffDef, bool shouldHave)
        {
            if (pawn == null || pawn.health == null || hediffDef == null) return;

            bool has = pawn.health.hediffSet.HasHediff(hediffDef);

            if (shouldHave && !has)
            {
                pawn.health.AddHediff(hediffDef);
            }
            else if (!shouldHave && has)
            {
                Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (h != null) pawn.health.RemoveHediff(h);
            }
        }

        /// <summary>
        /// 【通用能力分发】安全地为 Pawn 添加或移除指定的技能 (Ability)
        /// </summary>
        public static void ToggleAbility(Pawn pawn, AbilityDef abilityDef, bool shouldHave)
        {
            if (pawn == null || pawn.abilities == null || abilityDef == null) return;

            bool has = pawn.abilities.GetAbility(abilityDef) != null;

            if (shouldHave && !has)
            {
                pawn.abilities.GainAbility(abilityDef);
            }
            else if (!shouldHave && has)
            {
                pawn.abilities.RemoveAbility(abilityDef);
            }
        }

        /// <summary>
        /// 确保血脉字典中存在最低 1% 的保底，防止极端数值归零
        /// </summary>
        public static void EnsureBloodlineFloor(Dictionary<string, float> bloodlineComposition)
        {
            if (bloodlineComposition == null || bloodlineComposition.Count == 0) return;

            bool changed = false;
            float minThreshold = 0.01f;
            List<string> keys = bloodlineComposition.Keys.ToList();

            foreach (var key in keys)
            {
                if (bloodlineComposition[key] > 0f && bloodlineComposition[key] < minThreshold)
                {
                    bloodlineComposition[key] = minThreshold;
                    changed = true;
                }
            }

            if (changed)
            {
                float total = bloodlineComposition.Values.Sum();
                if (total > 0f)
                {
                    foreach (var key in keys)
                    {
                        bloodlineComposition[key] /= total;
                    }
                }
            }
        }
    }
}