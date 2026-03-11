using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;
using RavenRace.Features.FusangOrganization.UI;
using RavenRace.Features.RavenRite.Rite_Promotion.Purification.Comps;

namespace RavenRace
{
    public class ITab_Bloodline : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(350f, 450f);
        private Vector2 scrollPosition = Vector2.zero;

        private CompBloodline cachedComp;
        private CompPurification cachedPurComp;

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

        private CompPurification PurComp
        {
            get
            {
                Pawn pawn = base.SelPawn;
                if (pawn == null) return null;
                if (cachedPurComp != null && cachedPurComp.parent == pawn) return cachedPurComp;
                cachedPurComp = pawn.TryGetComp<CompPurification>();
                return cachedPurComp;
            }
        }

        public override bool IsVisible => Comp != null || PurComp != null;

        protected override void FillTab()
        {
            Rect mainRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            FusangUIStyle.DrawPanel(mainRect);

            CompBloodline comp = this.Comp;
            CompPurification purComp = this.PurComp;

            if (comp == null && purComp == null)
            {
                Widgets.Label(mainRect, "错误: 未找到血脉或纯化组件。");
                return;
            }

            Rect contentRect = mainRect.ContractedBy(15f);
            float currentY = contentRect.y;

            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Rect titleRect = new Rect(contentRect.x, currentY, contentRect.width, 30f);
            Widgets.Label(titleRect, "RavenRace_Bloodline".Translate());
            currentY += 35f;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // 读取独立金乌组件的数据
            if (purComp != null)
            {
                DrawConcentrationBar(contentRect.x, ref currentY, contentRect.width, purComp.GoldenCrowConcentration, purComp.currentPurificationStage);
                currentY += 15f;
            }

            GUI.color = FusangUIStyle.BorderColor;
            Widgets.DrawLineHorizontal(contentRect.x, currentY, contentRect.width);
            currentY += 15f;
            GUI.color = Color.white;

            if (comp != null)
            {
                Rect compositionLabelRect = new Rect(contentRect.x, currentY, contentRect.width, 22f);
                Widgets.Label(compositionLabelRect, "RavenRace_BloodlineComposition".Translate() + ":");
                currentY += 26f;

                Rect scrollViewOuterRect = new Rect(contentRect.x, currentY, contentRect.width, contentRect.yMax - currentY);

                var sortedBloodlines = comp.BloodlineComposition.Where(kv => kv.Value > 0.001f)
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
        }

        // 修改：增加传入纯化阶段，用于UI提示
        private void DrawConcentrationBar(float x, ref float y, float width, float concentration, int stage)
        {
            Rect labelRect = new Rect(x, y, width, 22f);
            Widgets.Label(labelRect, "RavenRace_GoldenCrowConcentration".Translate() + ": " + concentration.ToStringPercent("F1"));
            y += 24f;

            Rect barRect = new Rect(x, y, width, 22f);
            Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));

            Rect fillRect = barRect.ContractedBy(2f);
            fillRect.width *= concentration;
            Widgets.DrawBoxSolid(fillRect, FusangUIStyle.MainColor_Gold);

            GUI.color = FusangUIStyle.MainColor_DarkGold;
            Widgets.DrawBox(barRect, 1);
            GUI.color = Color.white;

            TooltipHandler.TipRegion(barRect, $"金乌纯度: {concentration:P2}\n纯化阶段: {stage}");
            y += 24f;
        }

        private void DrawCompositionRow(Rect rect, string raceDefName, float percent)
        {
            Rect barBgRect = rect;
            barBgRect.width *= percent;
            Widgets.DrawBoxSolid(barBgRect, new Color(0.4f, 0.35f, 0.2f, 0.3f));

            Rect labelRect = rect.LeftPart(0.7f).ContractedBy(4f, 0);
            string displayLabel = GetBloodlineDisplayLabel(raceDefName);
            Widgets.Label(labelRect, displayLabel);

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
            string translationKey = $"RavenRace_Bloodline_{raceDefName}";
            if (translationKey.CanTranslate())
            {
                return translationKey.Translate();
            }

            ThingDef raceDef = DefDatabase<ThingDef>.GetNamedSilentFail(raceDefName);
            if (raceDef != null) return raceDef.LabelCap;

            return raceDefName;
        }
    }
}