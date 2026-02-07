using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Text;

namespace RavenRace
{
    public class Hediff_SoulAltarBonus : HediffWithComps
    {
        // 存储属性加成列表 <StatDefName, Value>
        private Dictionary<string, float> storedStats = new Dictionary<string, float>();

        // 缓存生成的 Stage
        private HediffStage curStage;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref storedStats, "storedStats", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && storedStats == null)
            {
                storedStats = new Dictionary<string, float>();
            }
        }

        public void AddStat(StatDef stat, float value)
        {
            if (storedStats == null) storedStats = new Dictionary<string, float>();

            if (storedStats.ContainsKey(stat.defName))
            {
                storedStats[stat.defName] += value;
            }
            else
            {
                storedStats.Add(stat.defName, value);
            }
            
            // 清除缓存，强制重新生成 Stage
            curStage = null;
            pawn?.health?.Notify_HediffChanged(this);
        }

        public override HediffStage CurStage
        {
            get
            {
                // 如果缓存为空，则生成新的 Stage
                if (curStage == null)
                {
                    curStage = new HediffStage();
                    curStage.statOffsets = new List<StatModifier>();

                    if (storedStats != null && storedStats.Count > 0)
                    {
                        foreach (var kvp in storedStats)
                        {
                            StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(kvp.Key);
                            if (stat != null)
                            {
                                curStage.statOffsets.Add(new StatModifier { stat = stat, value = kvp.Value });
                            }
                        }
                    }
                }
                return curStage;
            }
        }

        public override bool ShouldRemove => false; // 永不自动移除

        public override string LabelInBrackets => "已激活";

        public override string TipStringExtra
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(base.TipStringExtra);

                if (storedStats != null && storedStats.Count > 0)
                {
                    sb.AppendLine("\n来自祭坛的赐福:");
                    foreach (var kvp in storedStats)
                    {
                        StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(kvp.Key);
                        if (stat != null)
                        {
                            // 格式化显示 (百分比或数值)
                            string valStr = stat.toStringStyle == ToStringStyle.PercentZero
                                ? kvp.Value.ToStringPercent()
                                : kvp.Value.ToStringByStyle(stat.toStringStyle);

                            string sign = kvp.Value > 0 ? "+" : "";
                            sb.AppendLine($" - {stat.LabelCap}: {sign}{valStr}");
                        }
                    }
                }
                return sb.ToString();
            }
        }
    }
}