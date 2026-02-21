using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using RavenRace.Compat.MoeLotl; // 引用

namespace RavenRace.Features.Bloodline
{
    public class CompProperties_Bloodline : CompProperties
    {
        public CompProperties_Bloodline()
        {
            this.compClass = typeof(CompBloodline);
        }
    }

    /// <summary>
    /// 血脉组件 - 核心数据定义
    /// </summary>
    public partial class CompBloodline : ThingComp
    {
        // 核心数据
        private Dictionary<string, float> bloodlineComposition = new Dictionary<string, float>();
        private float goldenCrowConcentration = 0f;

        public Dictionary<string, float> BloodlineComposition => bloodlineComposition;

        public float GoldenCrowConcentration
        {
            get => goldenCrowConcentration;
            set => goldenCrowConcentration = Mathf.Clamp01(value);
        }

        public Pawn Pawn => (Pawn)this.parent;

        // 在类中定义两个临时列表用于存档
        private List<string> tmpBloodlineKeys;
        private List<float> tmpBloodlineValues;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref goldenCrowConcentration, "goldenCrowConcentration", 0f);

            // 【修改】传入临时列表，防止红字 "You need to provide working lists..."
            Scribe_Collections.Look(ref bloodlineComposition, "bloodlineComposition", LookMode.Value, LookMode.Value, ref tmpBloodlineKeys, ref tmpBloodlineValues);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (bloodlineComposition == null) bloodlineComposition = new Dictionary<string, float>();
            }

            // ============================================================
            // [核心修复] 手动接管萌螈数据存档
            // 因为萌螈组件是动态添加的，不在XML里，原版Load不会处理它。
            // 我们必须在这里手动 Scribe 它的数据，把它存到 Bloodline 组件的数据块里。
            // ============================================================
            if (RavenRaceMod.Settings.enableMoeLotlCompat)
            {
                // 注意：这里需要传入 Pawn，因为 Utility 需要检查 Pawn 身上的组件
                MoeLotlCompatUtility.ExposeCultivationData(this.Pawn);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!RavenRaceMod.Settings.enableDebugMode) return null;

            if (bloodlineComposition == null) return "Bloodline: Error(Null)";

            try
            {
                string bloodlineStr = string.Join(", ", bloodlineComposition.Select(kv => $"{kv.Key}:{kv.Value:P0}"));
                return $"Golden Crow: {goldenCrowConcentration:P1}\nBloodlines: {bloodlineStr}";
            }
            catch
            {
                return "Bloodline: Error(Format)";
            }
        }
    }
}