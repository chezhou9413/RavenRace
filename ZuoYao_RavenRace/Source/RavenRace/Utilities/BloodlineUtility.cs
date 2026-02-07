using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace
{
    /// <summary>
    /// BloodlineUtility - 血脉系统工具方法
    /// 职责：
    /// - 血脉浓度计算
    /// - 遗传规则处理
    /// - 血脉特性判定
    /// </summary>
    public static class BloodlineUtility
    {
        // [!!! 待实现 - 请勿删除此注释 !!!]
        // Phase 3（血脉系统）将实现以下方法：
        // - CalculateInheritedConcentration(父母浓度): 计算遗传浓度
        // - GetBloodlineTraits(Pawn): 获取血脉带来的特性
        // - CanMutate(Pawn): 判断是否可以发生血脉突变

        /// <summary>
        /// 计算两个父母的金乌血脉浓度遗传到后代的浓度
        /// </summary>
        public static float CalculateInheritedConcentration(float parent1Concentration, float parent2Concentration)
        {
            // TODO: Phase 3实现
            // 基础公式：(父浓度 + 母浓度) / 2 * 遗传强度系数
            return 0f;
        }

        /// <summary>
        /// 判断是否满足血脉突变条件
        /// </summary>
        public static bool CanMutate(Pawn pawn, float currentConcentration)
        {
            // TODO: Phase 3实现
            return false;
        }

        /// <summary>
        /// 应用血脉突变
        /// </summary>
        public static void ApplyMutation(Pawn pawn)
        {
            // TODO: Phase 3实现
        }

        /// <summary>
        /// 根据浓度获取血脉等级描述
        /// </summary>
        public static string GetConcentrationLabel(float concentration)
        {
            // TODO: Phase 3实现
            if (concentration >= 0.9f) return "BloodlineLevel_Pure".Translate();
            if (concentration >= 0.7f) return "BloodlineLevel_High".Translate();
            if (concentration >= 0.5f) return "BloodlineLevel_Medium".Translate();
            if (concentration >= 0.3f) return "BloodlineLevel_Low".Translate();
            if (concentration > 0f) return "BloodlineLevel_Trace".Translate();
            return "BloodlineLevel_None".Translate();
        }

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