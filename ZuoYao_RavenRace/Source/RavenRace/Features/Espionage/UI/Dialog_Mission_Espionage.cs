using System;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.FusangOrganization.UI;

namespace RavenRace.Features.Espionage.UI
{
    [StaticConstructorOnStartup]
    public class Dialog_Mission_Espionage : FusangWindowBase
    {
        public override Vector2 InitialSize => new Vector2(1100f, 750f);
        protected override float Margin => 0f;

        private Faction selectedFaction;
        private Faction lastDrawnFaction;
        private Vector2 scrollPosLeft = Vector2.zero;
        private Vector2 scrollPosRight = Vector2.zero;

        private static readonly Texture2D CloseXSmall = ContentFinder<Texture2D>.Get("UI/Widgets/CloseXSmall", true);
        private static readonly Texture2D IconBack = ContentFinder<Texture2D>.Get("UI/Widgets/BackArrow", false) ?? CloseXSmall;

        private Thing radio;

        public Dialog_Mission_Espionage(Thing radio = null) : base()
        {
            this.radio = radio;
            foreach (var f in Find.FactionManager.AllFactions)
            {
                if (!f.IsPlayer && !f.Hidden && (f.HostileTo(Faction.OfPlayer) || f.PlayerRelationKind == FactionRelationKind.Neutral))
                {
                    selectedFaction = f;
                    break;
                }
            }
        }

        // DoWindowContents 和 DrawTitleBar 保持不变... (省略)
        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);
            Rect titleRect = new Rect(0, 0, inRect.width, 45);
            DrawTitleBar(titleRect);
            float contentY = titleRect.yMax;
            float contentHeight = inRect.height - contentY;
            Rect leftRect = new Rect(0, contentY, 250, contentHeight);
            DrawLeftPanel(leftRect);
            Rect rightRect = new Rect(leftRect.xMax, contentY, inRect.width - leftRect.width, contentHeight);
            DrawRightPanel(rightRect);
        }

        private void DrawTitleBar(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);
            if (Widgets.ButtonImage(new Rect(10, 10, 24, 24), IconBack))
            {
                Close();
                Find.WindowStack.Add(new Dialog_Espionage_Menu(radio));
            }
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(rect, "RavenRace_Espionage_CommandNet".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            Rect closeRect = new Rect(rect.width - 34f, 6f, 30f, 30f);
            if (Widgets.ButtonImage(closeRect, CloseXSmall)) Close();
        }

        private void DrawLeftPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.08f));
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);
            Rect inner = rect.ContractedBy(10);
            Rect viewRect = new Rect(0, 0, inner.width - 16, 1000);
            Widgets.BeginScrollView(inner, ref scrollPosLeft, viewRect);
            try
            {
                Listing_Standard listing = new Listing_Standard();
                listing.Begin(viewRect);
                foreach (var f in Find.FactionManager.AllFactions)
                {
                    if (f.Hidden || f.IsPlayer || f.def.defName == "Fusang_Hidden") continue;
                    Rect btnRect = listing.GetRect(40);
                    bool isSelected = (selectedFaction == f);
                    if (isSelected) Widgets.DrawBoxSolid(btnRect, new Color(1f, 0.8f, 0.3f, 0.1f));

                    // [核心修复] 切换派系时的清理逻辑
                    if (Widgets.ButtonInvisible(btnRect))
                    {
                        if (selectedFaction != f)
                        {
                            // 清理旧派系的头像缓存
                            CleanupFactionCache(selectedFaction);
                            selectedFaction = f;
                        }
                    }

                    Rect iconRect = new Rect(btnRect.x + 5, btnRect.y + 5, 30, 30);
                    FactionUIUtility.DrawFactionIconWithTooltip(iconRect, f);
                    Rect labelRect = new Rect(iconRect.xMax + 10, btnRect.y, btnRect.width - 45, btnRect.height);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    GUI.color = isSelected ? FusangUIStyle.MainColor_Gold : Color.gray;
                    Widgets.Label(labelRect, f.Name);
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                    if (Mouse.IsOver(btnRect) && !isSelected) Widgets.DrawHighlight(btnRect);
                }
                listing.End();
            }
            finally { Widgets.EndScrollView(); }
        }

        // [新增] 辅助方法：清理缓存
        private void CleanupFactionCache(Faction faction)
        {
            if (faction == null) return;
            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            var data = comp.GetSpyData(faction);
            if (data != null && data.allOfficials != null)
            {
                foreach (var off in data.allOfficials)
                {
                    off.ClearCache();
                }
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            // 窗口关闭时也清理一下当前选中的，释放内存
            CleanupFactionCache(selectedFaction);
        }

        private void DrawRightPanel(Rect rect)
        {
            // ... (与上一版代码一致，无需修改，重点是 DrawLeftPanel 的切换逻辑)
            if (selectedFaction == null) return;

            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            var data = comp.GetSpyData(selectedFaction);

            if (data.allOfficials.Count > 0 && data.allOfficials[0].factionRef == null)
            {
                foreach (var off in data.allOfficials) off.factionRef = selectedFaction;
            }

            float infoHeight = 80f;
            Rect infoRect = new Rect(rect.x, rect.y, rect.width, infoHeight).ContractedBy(10);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(infoRect.x, infoRect.y, 300, 30), selectedFaction.Name);
            Text.Font = GameFont.Small;

            int level = data.InfiltrationLevel;
            string levelDesc = GetLevelDescription(level);

            string statusStr = $"渗透度: {data.infiltrationPoints:F1} / 100 (Lv{level}: {levelDesc})\n"
                               + "RavenRace_Espionage_ControlStatus".Translate(data.controlStatus.ToString());

            GUI.color = Color.gray;
            Widgets.Label(new Rect(infoRect.x, infoRect.y + 30, 500, 40), statusStr);
            GUI.color = Color.white;

            Rect barRect = new Rect(infoRect.x + 350, infoRect.y + 5, 200, 20);
            Widgets.FillableBar(barRect, data.infiltrationPoints / 100f, SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.6f, 1f)));

            Widgets.DrawLineHorizontal(rect.x, rect.y + infoHeight, rect.width);

            Rect graphRect = new Rect(rect.x, rect.y + infoHeight, rect.width, rect.height - infoHeight);

            if (selectedFaction != lastDrawnFaction)
            {
                EspionageGraphDrawer.RecalculateLayout(data.leaderOfficial);
                lastDrawnFaction = selectedFaction;
                Vector2 size = EspionageGraphDrawer.GetGraphSize();
                scrollPosRight = new Vector2((size.x - graphRect.width) / 2f, 0f);
            }

            Vector2 viewSize = EspionageGraphDrawer.GetGraphSize();
            viewSize.x = Mathf.Max(viewSize.x, graphRect.width);
            viewSize.y = Mathf.Max(viewSize.y, graphRect.height);

            Rect viewRect = new Rect(0, 0, viewSize.x, viewSize.y);
            Widgets.DrawBoxSolid(graphRect, new Color(0.05f, 0.05f, 0.07f));

            Widgets.BeginScrollView(graphRect, ref scrollPosRight, viewRect);
            try
            {
                DrawGrid(viewRect);
                EspionageGraphDrawer.DrawGraph(graphRect, scrollPosRight, data.leaderOfficial);
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 0: return "未知";
                case 1: return "初步接触";
                case 2: return "建立网络";
                case 3: return "深度渗透";
                case 4: return "核心掌控";
                case 5: return "绝对支配"; // [新增]
                default: return "???";
            }
        }

        private void DrawGrid(Rect rect)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.05f);
            float step = 50f;
            for (float x = 0; x < rect.width; x += step) Widgets.DrawLineVertical(x, 0, rect.height);
            for (float y = 0; y < rect.height; y += step) Widgets.DrawLineHorizontal(0, y, rect.width);
            GUI.color = Color.white;
        }
    }
}