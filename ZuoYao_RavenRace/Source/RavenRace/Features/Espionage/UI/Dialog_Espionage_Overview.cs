using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.FusangOrganization.UI;

namespace RavenRace.Features.Espionage.UI
{
    [StaticConstructorOnStartup]
    public class Dialog_Espionage_Overview : FusangWindowBase
    {
        public override Vector2 InitialSize => new Vector2(1100f, 750f);
        protected override float Margin => 0f;

        private Vector2 scrollPosSpies = Vector2.zero;
        private Vector2 scrollPosColonists = Vector2.zero;
        private Faction selectedTargetFaction;
        private Thing radio; // 用于回退

        private static readonly Texture2D IconSpyDefault;
        private static readonly Texture2D CloseXSmall;
        private static readonly Texture2D IconBack; // [修复3]

        static Dialog_Espionage_Overview()
        {
            IconSpyDefault = ContentFinder<Texture2D>.Get("UI/Icons/Medical/SurgeryOption", false)
                             ?? ContentFinder<Texture2D>.Get("UI/Icons/QuestionMark", true);
            CloseXSmall = ContentFinder<Texture2D>.Get("UI/Widgets/CloseXSmall", true);
            IconBack = ContentFinder<Texture2D>.Get("UI/Widgets/BackArrow", false) ?? CloseXSmall;
        }

        public Dialog_Espionage_Overview(Thing radio = null) : base()
        {
            this.radio = radio;
            selectedTargetFaction = Find.FactionManager.AllFactionsVisible
                .FirstOrDefault(f => !f.IsPlayer && f.def.defName != "Fusang_Hidden");
        }

        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            // 1. 标题栏
            Rect titleRect = new Rect(0, 0, inRect.width, 45);
            Widgets.DrawBoxSolid(titleRect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(titleRect, FusangUIStyle.BorderColor);

            // [修复3] 回退
            if (Widgets.ButtonImage(new Rect(10, 10, 24, 24), IconBack))
            {
                Close();
                Find.WindowStack.Add(new Dialog_Espionage_Menu(radio));
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(titleRect, "RavenRace_Espionage_Management".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            Rect closeRect = new Rect(titleRect.width - 34f, 6f, 30f, 30f);
            if (Widgets.ButtonImage(closeRect, CloseXSmall)) Close();

            // 2. 布局
            float contentY = 55f;
            float mainHeight = inRect.height - contentY - 10f;

            Rect leftRect = new Rect(15, contentY, inRect.width * 0.6f - 20, mainHeight);
            DrawActiveSpiesPanel(leftRect);

            Rect rightRect = new Rect(leftRect.xMax + 10, contentY, inRect.width - leftRect.xMax - 25, mainHeight);
            DrawDispatchPanel(rightRect);
        }

        private void DrawActiveSpiesPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.08f));
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            Rect header = new Rect(rect.x, rect.y, rect.width, 35);
            Widgets.DrawBoxSolid(header, new Color(1f, 1f, 1f, 0.05f));
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(header.x + 10, header.y, header.width, header.height), "RavenRace_Espionage_ActiveSpies".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect listRect = new Rect(rect.x, rect.y + 40, rect.width, rect.height - 45);
            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            var spies = comp.GetAllSpies();

            float rowHeight = 85f; // [修改] 加高以容纳更多信息
            float viewHeight = spies.Count * (rowHeight + 5);
            Rect viewRect = new Rect(0, 0, listRect.width - 16, viewHeight);

            Widgets.BeginScrollView(listRect.ContractedBy(5), ref scrollPosSpies, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            if (spies.Count == 0)
            {
                listing.Label("RavenRace_Espionage_NoActiveSpies".Translate());
            }
            else
            {
                foreach (var spy in spies)
                {
                    DrawSpyRow(listing, spy, rowHeight);
                    listing.Gap(5);
                }
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawSpyRow(Listing_Standard listing, SpyData spy, float height)
        {
            Rect rect = listing.GetRect(height);
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            Rect inner = rect.ContractedBy(5);

            // 1. 头像
            Rect iconRect = new Rect(inner.x, inner.y, height - 10, height - 10);
            if (spy.sourceType == SpySourceType.Colonist && spy.colonistRef != null)
            {
                Widgets.ThingIcon(iconRect, spy.colonistRef);
            }
            else
            {
                GUI.DrawTexture(iconRect, IconSpyDefault);
            }

            // 2. 信息
            float infoX = iconRect.xMax + 10;
            float infoW = 300f; // [修改] 加宽

            Text.Font = GameFont.Small;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(infoX, inner.y, infoW, 25), $"{spy.Label}");
            GUI.color = Color.white;

            // [修复6] On Mission 详细信息
            string statusStr = "";
            if (spy.state == SpyState.OnMission && spy.currentMission != null)
            {
                int ticksLeft = spy.currentMission.TicksRemaining;
                float daysLeft = ticksLeft / 60000f;
                string timeStr = daysLeft.ToString("0.0") + " Days".Translate();

                string targetName = spy.currentMission.targetOfficial != null
                    ? spy.currentMission.targetOfficial.Label
                    : spy.targetFaction.Name;

                // [修复2] 使用翻译键
                statusStr = "RavenRace_Mission_Executing_Detail".Translate(spy.currentMission.def.label, targetName);
                statusStr += $" | {"RavenRace_Mission_TimeLeft".Translate(timeStr)}";

                GUI.color = new Color(0.4f, 0.8f, 1f);
            }
            else
            {
                // TODO: 添加 SpyState 的 Translate 扩展方法，这里暂时硬编码Key演示
                statusStr = $"RavenRace_SpyState_{spy.state}".Translate();
                GUI.color = Color.gray;
            }

            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(infoX, inner.y + 22, infoW, 20), statusStr);

            string factionName = spy.targetFaction != null ? spy.targetFaction.Name : "Idle";
            GUI.color = Color.gray;
            Widgets.Label(new Rect(infoX, inner.y + 42, infoW, 20), "RavenRace_Espionage_SpyLoc".Translate(factionName));
            GUI.color = Color.white;

            // 3. 属性概览 [修复2: 翻译]
            float statsX = infoX + infoW + 10;
            float statsW = inner.width - statsX - 90;

            Rect expLabelRect = new Rect(statsX, inner.y, statsW, 20);
            Widgets.Label(expLabelRect, "RavenRace_Espionage_Exposure".Translate());

            Rect expBarRect = new Rect(statsX, inner.y + 20, statsW * 0.8f, 15);
            Widgets.FillableBar(expBarRect, spy.exposure / 100f, SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.2f, 0.2f)), BaseContent.BlackTex, false);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(expBarRect, $"{spy.exposure:F0}%"); // 显示具体数值
            Text.Anchor = TextAnchor.UpperLeft;

            // [修复2] 翻译属性名
            string statsText = $"{"RavenRace_Espionage_Stat_Infiltration".Translate()}:{spy.statInfiltration:F0}  {"RavenRace_Espionage_Stat_Operation".Translate()}:{spy.statOperation:F0}";
            Widgets.Label(new Rect(statsX, inner.y + 45, statsW, 20), statsText);

            // 4. 按钮
            Rect btnRect = new Rect(inner.xMax - 80, inner.y + (inner.height - 30) / 2, 75, 30);
            if (FusangUIStyle.DrawButton(btnRect, "RavenRace_Espionage_RecallBtn".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RavenRace_Espionage_RecallConfirm".Translate(spy.Label),
                    () =>
                    {
                        var comp = Find.World.GetComponent<WorldComponent_Espionage>();
                        comp.RecallSpy(spy, Find.CurrentMap);
                    },
                    true));
            }
        }

        private void DrawDispatchPanel(Rect rect)
        {
            // ... (与之前相同，省略重复代码以聚焦核心修改，请使用上一轮的 DrawDispatchPanel 逻辑) ...
            // 请确保 DrawDispatchPanel 里的文本使用了 Translate()
            // 示例： "RavenRace_Espionage_DispatchAction".Translate()
            // ...
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.08f));
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            Rect header = new Rect(rect.x, rect.y, rect.width, 35);
            Widgets.DrawBoxSolid(header, new Color(1f, 1f, 1f, 0.05f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(header, "RavenRace_Espionage_DispatchAction".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            Rect content = rect.ContractedBy(10);
            content.yMin += 35;

            Rect factionBtn = new Rect(content.x, content.y, content.width, 40);
            string label = selectedTargetFaction != null ? $"{selectedTargetFaction.Name}" : "RavenRace_Espionage_SelectTargetFaction".Translate().ToString();

            if (Widgets.ButtonText(factionBtn, label))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (var f in Find.FactionManager.AllFactionsVisible)
                {
                    if (!f.IsPlayer && f.def.defName != "Fusang_Hidden")
                    {
                        list.Add(new FloatMenuOption(f.Name, () => selectedTargetFaction = f));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }

            Rect listRect = new Rect(content.x, content.y + 50, content.width, content.height - 50);
            Widgets.DrawBoxSolid(listRect, new Color(0, 0, 0, 0.2f));

            Widgets.BeginScrollView(listRect, ref scrollPosColonists, new Rect(0, 0, listRect.width - 16, 1000));
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(new Rect(0, 0, listRect.width - 16, 1000));

            var colonists = Find.CurrentMap.mapPawns.FreeColonists
                .Where(p => !p.Dead && !p.Downed && !p.IsQuestLodger()).ToList();

            if (colonists.Count == 0)
            {
                listing.Label("RavenRace_Espionage_NoColonists".Translate());
            }
            else
            {
                foreach (var pawn in colonists)
                {
                    DrawColonistRow(listing, pawn);
                    listing.Gap(5);
                }
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawColonistRow(Listing_Standard listing, Pawn pawn)
        {
            Rect rect = listing.GetRect(50);
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.03f));

            Rect iconRect = new Rect(rect.x + 5, rect.y + 5, 40, 40);
            Widgets.ThingIcon(iconRect, pawn);

            Rect nameRect = new Rect(iconRect.xMax + 10, rect.y + 5, rect.width - 140, 20);
            Text.Font = GameFont.Small;
            Widgets.Label(nameRect, pawn.LabelShort);

            Rect jobRect = new Rect(iconRect.xMax + 10, rect.y + 25, rect.width - 140, 20);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(jobRect, pawn.story.TitleShort);
            GUI.color = Color.white;

            Rect btnRect = new Rect(rect.xMax - 85, rect.y + 10, 80, 30);
            bool canDispatch = selectedTargetFaction != null;

            if (FusangUIStyle.DrawButton(btnRect, "RavenRace_Espionage_DispatchBtn".Translate(), canDispatch))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RavenRace_Espionage_DispatchConfirm".Translate(pawn.LabelShort, selectedTargetFaction.Name),
                    () =>
                    {
                        var comp = Find.World.GetComponent<WorldComponent_Espionage>();
                        comp.DispatchColonist(pawn, selectedTargetFaction);
                    },
                    true));
            }
        }
    }
}