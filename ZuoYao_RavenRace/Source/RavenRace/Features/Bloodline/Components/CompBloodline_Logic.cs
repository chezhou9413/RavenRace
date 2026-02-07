using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace.Features.Bloodline
{
    /// <summary>
    /// 血脉组件 - 核心逻辑 (初始化、计算、验证)
    /// </summary>
    public partial class CompBloodline
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // 1. 初始化
            if (bloodlineComposition == null || bloodlineComposition.Count == 0)
            {
                InitializeBloodline();
            }

            // 2. 强制执行 1% 保底检查 (兼容旧存档)
            EnsureBloodlineFloor();

            // 3. 赋予能力 (调用 Compat 部分的方法)
            CheckAndGrantBloodlineAbilities();
        }

        public void InitializeBloodline()
        {
            if (bloodlineComposition == null) bloodlineComposition = new Dictionary<string, float>();
            if (bloodlineComposition.Count > 0) return;

            BloodlineManager.InitializePawnBloodline(this.Pawn, this);
        }

        /// <summary>
        /// [核心逻辑] 确保所有存在的血脉至少有 1% (0.01) 的占比
        /// </summary>
        private void EnsureBloodlineFloor()
        {
            if (bloodlineComposition == null || bloodlineComposition.Count == 0) return;

            bool changed = false;
            float minThreshold = 0.01f; // 1%

            // A. 检查是否有低于 1% 的
            List<string> keys = bloodlineComposition.Keys.ToList();
            foreach (var key in keys)
            {
                if (bloodlineComposition[key] > 0f && bloodlineComposition[key] < minThreshold)
                {
                    bloodlineComposition[key] = minThreshold;
                    changed = true;
                }
            }

            // B. 如果有变动，重新归一化 (Normalize) 使得总和为 1.0
            if (changed)
            {
                float total = bloodlineComposition.Values.Sum();
                if (total > 0f)
                {
                    // 重新计算比例
                    foreach (var key in keys)
                    {
                        bloodlineComposition[key] /= total;
                    }
                }
            }
        }

        public void SetBloodlineComposition(Dictionary<string, float> newComposition)
        {
            if (newComposition == null) return;
            if (bloodlineComposition == null) bloodlineComposition = new Dictionary<string, float>();

            bloodlineComposition.Clear();
            foreach (var kvp in newComposition)
            {
                bloodlineComposition.Add(kvp.Key, kvp.Value);
            }
            EnsureBloodlineFloor(); // 设置新数据时也要保底
        }
    }
}