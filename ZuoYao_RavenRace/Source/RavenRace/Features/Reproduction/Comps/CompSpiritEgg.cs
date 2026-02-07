using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using RavenRace.Features.Bloodline;
using RimWorld;
using Verse;

namespace RavenRace.Features.Reproduction
{
    public class CompProperties_SpiritEgg : CompProperties
    {
        public CompProperties_SpiritEgg()
        {
            this.compClass = typeof(CompSpiritEgg);
        }
    }

    /// <summary>
    /// 灵卵核心组件 - 主入口
    /// [关键] 必须继承 ThingComp，且命名空间必须统一
    /// </summary>
    public partial class CompSpiritEgg : ThingComp
    {
        public override void PostPostMake()
        {
            base.PostPostMake();
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();

            // isIncubating 定义在 Data 文件中，只要命名空间一致就能访问
            if (isIncubating)
            {
                sb.AppendLine("IncubationProgress".Translate() + ": " + progress.ToStringPercent());

                float remainingTicks = TotalTicksNeeded * (1f - progress);
                if (remainingTicks > 0)
                {
                    sb.AppendLine("TimeLeft".Translate() + ": " + ((int)remainingTicks).ToStringTicksToPeriod());
                }
            }
            else
            {
                sb.AppendLine("状态: 等待激活");
            }

            if (warmthProgress > 0f)
            {
                sb.AppendLine($"温养度: {warmthProgress:P0}");
                if (warmthProgress >= 1.0f)
                {
                    sb.AppendLine(" (已达完美温养状态，孵化将获得加成)");
                }
            }

            sb.AppendLine("RavenRace_Mother".Translate() + ": " + (motherName ?? "Unknown"));
            sb.AppendLine("RavenRace_Father".Translate() + ": " + (fatherName ?? "Unknown"));
            sb.AppendLine("RavenRace_GoldenCrowConcentration".Translate() + ": " + goldenCrowConcentration.ToString("P1"));

            if (bloodlineComposition != null && bloodlineComposition.Count > 0)
            {
                sb.Append("RavenRace_BloodlineComposition".Translate() + ": ");
                var ordered = bloodlineComposition.OrderByDescending(x => x.Value);
                var strList = ordered.Select(x => $"{GetRaceLabel(x.Key)}({x.Value:P0})");
                sb.AppendLine(string.Join(", ", strList));
            }

            return sb.ToString().TrimEnd();
        }

        private string GetRaceLabel(string defName)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            return def != null ? def.LabelCap.ToString() : defName;
        }
    }
}