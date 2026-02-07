using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace
{
    /// <summary>
    /// BloodlineDef - 定义一种血脉
    /// 将游戏中的种族 (ThingDef) 映射到血脉系统中，并定义该血脉的基础属性。
    /// </summary>
    public class BloodlineDef : Def
    {
        /// <summary>
        /// 关联的种族定义
        /// </summary>
        public ThingDef raceDef;

        /// <summary>
        /// 该血脉的基础标签（例如：渡鸦、人类、鼠族）
        /// 用于UI显示
        /// </summary>
        public string labelShort;

        /// <summary>
        /// 是否为金乌血脉源头（如果是，将启用金乌浓度条）
        /// </summary>
        public bool isGoldenCrowSource = false;

        // 可以在这里添加更多属性，例如：
        // - 该血脉带来的属性修正
        // - 杂交时的优势/劣势权重

        /// <summary>
        /// 配置错误检查
        /// </summary>
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (raceDef == null)
            {
                yield return $"[RavenRace] BloodlineDef {defName} has null raceDef.";
            }
        }
    }
}