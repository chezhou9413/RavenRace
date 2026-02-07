using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace
{
    /// <summary>
    /// RavenModUtility - 模组通用工具方法
    /// 职责：
    /// - 模组兼容性检查
    /// - 全局日志和调试工具
    /// - 资源加载和缓存
    /// - 通用数据转换
    /// </summary>
    public static class RavenModUtility
    {
        // [!!! 待实现 - 请勿删除此注释 !!!]
        // 以下方法将在各Phase中逐步实现

        /// <summary>
        /// 输出详细日志（仅在启用详细日志时）
        /// </summary>
        public static void LogVerbose(string message)
        {
            if (RavenRaceMod.Settings != null && RavenRaceMod.Settings.enableVerboseLogging)
            {
                Log.Message($"[RavenRace] {message}");
            }
        }

        /// <summary>
        /// 输出调试日志（仅在调试模式下）
        /// </summary>
        public static void LogDebug(string message)
        {
            if (RavenRaceMod.Settings != null && RavenRaceMod.Settings.enableDebugMode)
            {
                Log.Warning($"[RavenRace DEBUG] {message}");
            }
        }

        /// <summary>
        /// 检查是否安装了指定Mod
        /// </summary>
        public static bool IsModActive(string packageId)
        {
            return ModsConfig.IsActive(packageId);
        }

        /// <summary>
        /// 检查HAR是否已安装
        /// </summary>
        public static bool IsHARActive()
        {
            return IsModActive("erdelf.HumanoidAlienRaces");
        }

        /// <summary>
        /// 获取安全的浮点数（避免NaN和Infinity）
        /// </summary>
        public static float SafeFloat(float value, float defaultValue = 0f)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                LogDebug($"Invalid float value detected: {value}, using default: {defaultValue}");
                return defaultValue;
            }
            return value;
        }

        /// <summary>
        /// 将百分比转换为0-1的浮点数
        /// </summary>
        public static float PercentToFloat(float percent)
        {
            return SafeFloat(percent / 100f);
        }

        /// <summary>
        /// 将0-1的浮点数转换为百分比
        /// </summary>
        public static float FloatToPercent(float value)
        {
            return SafeFloat(value * 100f);
        }

        /// <summary>
        /// 获取随机权重选择的索引
        /// </summary>
        public static int GetWeightedRandomIndex(List<float> weights)
        {
            if (weights == null || weights.Count == 0)
            {
                return -1;
            }

            float totalWeight = weights.Sum();
            if (totalWeight <= 0f)
            {
                return 0;
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return i;
                }
            }

            return weights.Count - 1;
        }
    }
}