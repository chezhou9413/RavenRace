using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;
using RavenRace.Features.FusangOrganization.UI; // <-- 核心修改：引用扶桑UI风格

namespace RavenRace
{
    public class ITab_Bloodline : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(350f, 450f); // 稍微调整窗口大小以适应新风格
        private Vector2 scrollPosition = Vector2.zero; // 用于滚动视图

        private CompBloodline cachedComp;

        public ITab_Bloodline()
        {
            this.size = WinSize;
            this.labelKey = "RavenRace_Bloodline";
        }

        private CompBloodline Comp
        {
            get
            {
                Pawn pawn = base.SelPawn;
                if (pawn == null) return null;
                if (cachedComp != null && cachedComp.parent == pawn) return cachedComp;
                cachedComp = pawn.TryGetComp<CompBloodline>();
                return cachedComp;
            }
        }

        public override bool IsVisible => Comp != null;

        protected override void FillTab()
        {
            // --- 使用扶桑风格重构整个UI ---

            Rect mainRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            // 绘制背景和边框
            FusangUIStyle.DrawPanel(mainRect);

            CompBloodline comp = this.Comp;
            if (comp == null)
            {
                Widgets.Label(mainRect, "错误: 未找到血脉组件。");
                return;
            }

            // 使用一个内部Rect来管理布局
            Rect contentRect = mainRect.ContractedBy(15f);
            float currentY = contentRect.y;

            // --- 1. 标题 ---
            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Rect titleRect = new Rect(contentRect.x, currentY, contentRect.width, 30f);
            Widgets.Label(titleRect, "RavenRace_Bloodline".Translate());
            currentY += 35f;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // --- 2. 金乌浓度条 ---
            DrawConcentrationBar(contentRect.x, ref currentY, contentRect.width, comp.GoldenCrowConcentration);
            currentY += 15f;

            // --- 3. 分割线 ---
            GUI.color = FusangUIStyle.BorderColor;
            Widgets.DrawLineHorizontal(contentRect.x, currentY, contentRect.width);
            currentY += 15f;
            GUI.color = Color.white;

            // --- 4. 血脉组成 (带滚动视图) ---
            Rect compositionLabelRect = new Rect(contentRect.x, currentY, contentRect.width, 22f);
            Widgets.Label(compositionLabelRect, "RavenRace_BloodlineComposition".Translate() + ":");
            currentY += 26f;

            Rect scrollViewOuterRect = new Rect(contentRect.x, currentY, contentRect.width, contentRect.yMax - currentY);

            var sortedBloodlines = comp.BloodlineComposition.Where(kv => kv.Value > 0.001f) // 过滤掉极小值
                                                             .OrderByDescending(kv => kv.Value)
                                                             .ToList();

            float rowHeight = 26f;
            float viewRectHeight = sortedBloodlines.Count * rowHeight;
            Rect viewRect = new Rect(0, 0, scrollViewOuterRect.width - 16f, viewRectHeight);

            Widgets.BeginScrollView(scrollViewOuterRect, ref scrollPosition, viewRect);

            float scrollY = 0;
            foreach (var entry in sortedBloodlines)
            {
                Rect rowRect = new Rect(0, scrollY, viewRect.width, rowHeight - 2f);
                DrawCompositionRow(rowRect, entry.Key, entry.Value);
                scrollY += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawConcentrationBar(float x, ref float y, float width, float concentration)
        {
            // 标签
            Rect labelRect = new Rect(x, y, width, 22f);
            Widgets.Label(labelRect, "RavenRace_GoldenCrowConcentration".Translate() + ": " + concentration.ToStringPercent("F1"));
            y += 24f;

            // 进度条
            Rect barRect = new Rect(x, y, width, 22f);
            Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f, 0.5f)); // 暗色背景

            Rect fillRect = barRect.ContractedBy(2f);
            fillRect.width *= concentration;
            Widgets.DrawBoxSolid(fillRect, FusangUIStyle.MainColor_Gold); // 金色填充

            GUI.color = FusangUIStyle.MainColor_DarkGold;
            Widgets.DrawBox(barRect, 1); // 金色边框
            GUI.color = Color.white;

            TooltipHandler.TipRegion(barRect, "RavenRace_BloodlinePurity".Translate() + ": " + concentration.ToStringPercent("F2"));
            y += 24f;
        }

        private void DrawCompositionRow(Rect rect, string raceDefName, float percent)
        {
            // 绘制微型背景条
            Rect barBgRect = rect;
            barBgRect.width *= percent;
            Widgets.DrawBoxSolid(barBgRect, new Color(0.4f, 0.35f, 0.2f, 0.3f));

            // 左侧标签
            Rect labelRect = rect.LeftPart(0.7f).ContractedBy(4f, 0);
            string displayLabel = GetBloodlineDisplayLabel(raceDefName);
            Widgets.Label(labelRect, displayLabel);

            // 右侧百分比
            Rect percentRect = rect.RightPart(0.3f).ContractedBy(4f, 0);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = FusangUIStyle.TextColor;
            Widgets.Label(percentRect, percent.ToStringPercent("F1"));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(rect, $"{displayLabel}: {percent:P2}");
        }

        private string GetBloodlineDisplayLabel(string raceDefName)
        {
            // 优先尝试翻译特殊Key
            string translationKey = $"RavenRace_Bloodline_{raceDefName}";
            if (translationKey.CanTranslate())
            {
                return translationKey.Translate();
            }

            // 其次尝试从RaceDef获取
            ThingDef raceDef = DefDatabase<ThingDef>.GetNamedSilentFail(raceDefName);
            if (raceDef != null) return raceDef.LabelCap;

            // 最后的兜底
            return raceDefName;
        }
    }
}