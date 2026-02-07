using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.FusangOrganization.UI;
using RavenRace.Features.Espionage.Workers;

namespace RavenRace.Features.Espionage.UI
{
    public class Dialog_MissionSelection : FusangWindowBase
    {
        public override Vector2 InitialSize => new Vector2(600f, 700f);

        private Faction targetFaction;
        private OfficialData targetOfficial;
        private SpyData selectedSpy;

        private Vector2 scrollPosition = Vector2.zero;

        public Dialog_MissionSelection(Faction faction, OfficialData official = null) : base()
        {
            this.targetFaction = faction;
            this.targetOfficial = official;
            this.doCloseX = true;

            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            var data = comp.GetSpyData(faction);
            // 找到一个空闲的驻扎间谍
            this.selectedSpy = data.activeSpies.Find(s => s.state == SpyState.Infiltrating);
        }

        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            // 标题栏
            float headerHeight = 80f;
            Rect headerRect = new Rect(inRect.x + 20, inRect.y + 15, inRect.width - 40, headerHeight);

            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, headerRect.width, 30), "RavenRace_Mission_SelectTitle".Translate(targetFaction.Name));

            if (targetOfficial != null)
            {
                Text.Font = GameFont.Small;
                string targetName = targetOfficial.isKnown ? targetOfficial.Label : "RavenRace_Espionage_UnknownTarget".Translate().ToString();
                Widgets.Label(new Rect(headerRect.x, headerRect.y + 35, headerRect.width, 25), "RavenRace_Mission_Target".Translate(targetName));
            }
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            GUI.color = FusangUIStyle.BorderColor;
            Widgets.DrawLineHorizontal(headerRect.x, headerRect.yMax, headerRect.width);
            GUI.color = Color.white;

            if (selectedSpy == null)
            {
                GUI.color = Color.red;
                Widgets.Label(new Rect(headerRect.x, headerRect.yMax + 10, headerRect.width, 30), "RavenRace_Mission_NoSpy".Translate());
                Widgets.Label(new Rect(headerRect.x, headerRect.yMax + 40, headerRect.width, 30), "RavenRace_Mission_PleaseDeploy".Translate());
                return;
            }

            Rect spyInfoRect = new Rect(headerRect.x, headerRect.yMax + 10, headerRect.width, 50);
            string spyInfo = "RavenRace_Mission_Executor".Translate(selectedSpy.Label) + " | " +
                             "RavenRace_Mission_CurrentExposure".Translate(selectedSpy.exposure.ToString("F0"));
            Widgets.Label(spyInfoRect, spyInfo);

            float listY = spyInfoRect.yMax + 10f;
            Rect listRect = new Rect(inRect.x + 10, listY, inRect.width - 20, inRect.height - listY - 10);

            var allMissions = DefDatabase<EspionageMissionDef>.AllDefsListForReading;
            float viewHeight = 0f;
            foreach (var def in allMissions)
            {
                if (!ShouldShowMission(def)) continue;
                viewHeight += 110f; // [修改] 增加高度以显示前置条件
            }

            Rect viewRect = new Rect(0, 0, listRect.width - 16, viewHeight);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            foreach (var def in allMissions)
            {
                if (!ShouldShowMission(def)) continue;
                DrawMissionCard(listing, def);
                listing.Gap(10);
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private bool ShouldShowMission(EspionageMissionDef def)
        {
            if (def.requiresTargetOfficial && targetOfficial == null) return false;

            // [逻辑修复] 如果当前针对特定官员，但也允许显示通用的搜集情报任务
            if (targetOfficial != null && !def.requiresTargetOfficial) return true;

            return true;
        }

        private void DrawMissionCard(Listing_Standard listing, EspionageMissionDef def)
        {
            Rect rect = listing.GetRect(100f);
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            Rect inner = rect.ContractedBy(8);

            // 标题
            Text.Font = GameFont.Small;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width - 100, 25), def.label);

            // 描述
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(inner.x, inner.y + 25, inner.width - 100, 40), def.description);
            GUI.color = Color.white;

            // 消耗提示
            string costTip = "RavenRace_Mission_CostTip".Translate(def.costIntel, def.costInfluence);
            TooltipHandler.TipRegion(rect, costTip);

            // 按钮与验证
            Rect btnRect = new Rect(inner.xMax - 90, inner.y + 20, 80, 40);
            string reason;
            bool canStart = def.Worker.CanStartNow(targetFaction, targetOfficial, out reason);

            if (FusangUIStyle.DrawButton(btnRect, "RavenRace_Mission_ExecuteBtn".Translate(), canStart))
            {
                def.Worker.StartMission(targetFaction, selectedSpy, targetOfficial);
                Close();
            }

            if (!canStart)
            {
                TooltipHandler.TipRegion(btnRect, reason);

                // [新增] 在卡片底部显著显示不可用原因 (特别是前置条件)
                GUI.color = new Color(1f, 0.4f, 0.4f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(inner.x, inner.yMax - 20, inner.width - 100, 20), "前置未满足: " + reason);
                GUI.color = Color.white;
            }
        }
    }
}