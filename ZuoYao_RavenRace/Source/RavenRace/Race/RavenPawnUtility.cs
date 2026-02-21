using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace
{
    /// <summary>
    /// RavenPawnUtility - 渡鸦族Pawn相关的工具方法
    /// 职责：
    /// - 判断一个Pawn是否为渡鸦族/金乌族
    /// - 获取Pawn的种族特定属性
    /// - 处理种族特定的Pawn行为
    /// - 翅膀状态和飞行能力检查
    /// </summary>
    public static class RavenPawnUtility
    {
        // [!!! 待实现 - 请勿删除此注释 !!!]
        // Phase 1将实现以下方法：
        // - IsRaven(Pawn pawn): 判断是否为渡鸦族
        // - IsGoldenCrow(Pawn pawn): 判断是否为金乌族
        // - HasWings(Pawn pawn): 判断是否拥有翅膀
        // - GetGoldenCrowConcentration(Pawn pawn): 获取金乌血脉浓度

        /// <summary>
        /// 判断一个Pawn是否为渡鸦族
        /// </summary>
        public static bool IsRaven(Pawn pawn)
        {
            // TODO: Phase 1实现
            return false;
        }

        /// <summary>
        /// 判断一个Pawn是否为金乌族（完全转化）
        /// </summary>
        public static bool IsGoldenCrow(Pawn pawn)
        {
            // TODO: Phase 2实现
            return false;
        }

        /// <summary>
        /// 判断一个Pawn是否拥有可用的翅膀
        /// </summary>
        public static bool HasFunctionalWings(Pawn pawn)
        {
            // TODO: Phase 1实现
            return false;
        }

        /// <summary>
        /// 获取Pawn的金乌血脉浓度（0-1）
        /// </summary>
        public static float GetGoldenCrowConcentration(Pawn pawn)
        {
            // TODO: Phase 3实现（血脉系统）
            return 0f;
        }

        /// <summary>
        /// 判断Pawn是否为渡鸦族或其杂交后代
        /// </summary>
        public static bool IsRavenOrHybrid(Pawn pawn)
        {
            // TODO: Phase 5实现（杂交系统）
            return IsRaven(pawn);
        }
    }
}