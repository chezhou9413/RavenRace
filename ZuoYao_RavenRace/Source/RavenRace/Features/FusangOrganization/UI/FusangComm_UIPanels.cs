using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Operator;
using HarmonyLib;
using RavenRace.Features.Operator.UI;
using RavenRace.Features.Operator.Rewards;
using RavenRace.Features.Espionage.UI; // [新增] 引用间谍UI

namespace RavenRace.Features.FusangOrganization.UI
{
    [StaticConstructorOnStartup]
    public static class FusangComm_UIPanels
    {
        // 静态资源 (保持不变)
        private static readonly Texture2D IconSettings;
        private static readonly Texture2D IconHeart;
        private static readonly Texture2D IconSupplies;
        private static readonly Texture2D IconMilitary;
        private static readonly Texture2D IconIntel;
        private static readonly Texture2D IconInfluence;

        static FusangComm_UIPanels()
        {
            IconSettings = ContentFinder<Texture2D>.Get("UI/Buttons/InfoButton", false) ?? BaseContent.BadTex;
            IconHeart = ContentFinder<Texture2D>.Get("UI/Icons/HeartIcon", true);
            IconSupplies = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Supplies") ?? BaseContent.BadTex;
            IconMilitary = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Military") ?? BaseContent.BadTex;
            IconIntel = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Intel") ?? BaseContent.BadTex;
            IconInfluence = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Influence") ?? BaseContent.BadTex;
        }


        public static void DrawLeftPanel(Rect rect, Thing radio, Texture2D portrait, WorldComponent_OperatorManager opManager)
        {
            float portraitHeight = rect.width * (2330f / 2060f);
            Rect portraitFrame = new Rect(rect.x, rect.y, rect.width, portraitHeight);

            Widgets.DrawBoxSolid(portraitFrame, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(portraitFrame, FusangUIStyle.BorderColor);
            if (portrait != null) GUI.DrawTexture(portraitFrame.ContractedBy(4), portrait, ScaleMode.ScaleToFit);
            DrawExpressionSwitchButton(portraitFrame, opManager, radio);

            float statusY = portraitFrame.yMax + 10;
            Rect statusRect = new Rect(rect.x, statusY, rect.width, rect.height - portraitHeight - 10);

            DrawStatusAndGalgamePanel(statusRect);
        }

        private static void DrawStatusAndGalgamePanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(10));

            var opManager = Find.World.GetComponent<WorldComponent_OperatorManager>();

            GUI.color = FusangUIStyle.MainColor_Gold;
            listing.Label(":: 链路状态 & 交互 ::");
            GUI.color = FusangUIStyle.BorderColor;
            listing.GapLine();
            GUI.color = FusangUIStyle.TextColor;

            DrawFavorabilityBar(listing, opManager);
            listing.Gap(5);
            DrawTradeCooldown(listing);

            listing.GapLine();
            listing.Gap(10);

            // 按钮部分保持不变
            Rect btnRect1 = listing.GetRect(35f);
            if (FusangUIStyle.DrawButton(btnRect1, "赠送礼物"))
            {
                Find.WindowStack.Add(new Dialog_GiftGiving());
            }
            TooltipHandler.TipRegion(btnRect1, "向左爻赠送殖民地的物品以提升好感度。");
            listing.Gap(5);

            Rect btnRect2 = listing.GetRect(35f);
            if (FusangUIStyle.DrawButton(btnRect2, "请求回礼"))
            {
                List<RewardDef> availableRewards = DefDatabase<RewardDef>.AllDefs
                    .Where(r => opManager.unlockedRewardDefs.Contains(r.defName))
                    .ToList();

                if (availableRewards.Any())
                {
                    Find.WindowStack.Add(new Dialog_RequestReward(availableRewards));
                }
                else
                {
                    Messages.Message("当前没有可领取的回礼。", MessageTypeDefOf.NeutralEvent);
                }
            }
            TooltipHandler.TipRegion(btnRect2, "领取达到新好感度阶段的特殊奖励。");
            listing.Gap(5);

            Rect btnRect3 = listing.GetRect(35f);
            if (FusangUIStyle.DrawButton(btnRect3, "内衣收藏"))
            {
                Find.WindowStack.Add(new Dialog_UnderwearCollection());
            }
            TooltipHandler.TipRegion(btnRect3, "查看已收集的左爻的秘密赠礼。");

            listing.End();
        }

        /// <summary>
        /// 绘制右侧的功能面板。
        /// </summary>
        public static void DrawFunctionPanel(Rect rect, Dialog_FusangComm window)
        {
            var radio = Traverse.Create(window).Field("radio").GetValue<Thing>();
            var fusangFaction = Traverse.Create(window).Field("fusangFaction").GetValue<Faction>();
            var fusangWorldComp = Traverse.Create(window).Field("fusangWorldComp").GetValue<WorldComponent_Fusang>();

            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            Rect titleRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 20);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(titleRect, ":: 功能面板 ::");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect buttonsArea = new Rect(rect.x, rect.y + 25, rect.width, rect.height - 25).ContractedBy(10);
            float btnWidth = (buttonsArea.width - 10) / 2f;
            float btnHeight = (buttonsArea.height - 10) / 2f;

            Rect tradeRect = new Rect(buttonsArea.x, buttonsArea.y, btnWidth, btnHeight);
            Rect espionageRect = new Rect(tradeRect.xMax + 10, buttonsArea.y, btnWidth, btnHeight); // 原 ScanRect
            Rect missionRect = new Rect(buttonsArea.x, tradeRect.yMax + 10, btnWidth, btnHeight);
            Rect supportRect = new Rect(missionRect.xMax + 10, tradeRect.yMax + 10, btnWidth, btnHeight);

            bool onCooldown = !fusangWorldComp.CanTradeNow();
            string tradeLabel = onCooldown ? $"物流整备\n({fusangWorldComp.GetTicksUntilTrade() / 60000f:F1}天)" : "物资交换协议";

            if (FusangUIStyle.DrawButton(tradeRect, tradeLabel, !onCooldown))
            {
                IncidentParms parms = new IncidentParms { target = radio.Map, faction = fusangFaction, forced = true };
                IncidentDef def = DefDatabase<IncidentDef>.GetNamed("Raven_Incident_TraderCaravanArrival");
                if (def.Worker.TryExecute(parms))
                {
                    int cooldownTicks = (int)(RavenRaceMod.Settings.tradeCaravanCooldownDays * 60000f);
                    fusangWorldComp.lastTradeTick = Find.TickManager.TicksGame;
                    fusangWorldComp.tradeCooldownTicks = cooldownTicks;
                    Messages.Message("扶桑商队已派出，请留意地图边缘。", MessageTypeDefOf.TaskCompletion);
                    window.Close();
                }
                else { Messages.Message("商队派遣失败：无法找到安全的进入路径。", MessageTypeDefOf.RejectInput); }
            }

            // [修改] 指向新的门户菜单
            if (FusangUIStyle.DrawButton(espionageRect, "间谍与派系"))
            {
                Find.WindowStack.Add(new Dialog_Espionage_Menu());
                window.Close(); 
            }

            if (FusangUIStyle.DrawButton(missionRect, "任务面板")) { window.Close(); Find.WindowStack.Add(new Dialog_FusangMissionBoard(radio)); }
            if (FusangUIStyle.DrawButton(supportRect, "捐赠支援")) { window.Close(); Find.WindowStack.Add(new Dialog_FusangSupport(radio)); }
        }

        // DrawTradeCooldown, DrawFavorabilityBar 等辅助方法保持不变 (省略以节省篇幅，请保留原有代码)
        // ...

        private static void DrawTradeCooldown(Listing_Standard listing)
        {
            var comp = Find.World.GetComponent<WorldComponent_Fusang>();
            string tradeStatus;
            if (comp.CanTradeNow())
            {
                GUI.color = FusangUIStyle.MainColor_Gold;
                tradeStatus = "RavenRace_Status_Ready".Translate();
            }
            else
            {
                GUI.color = Color.gray;
                float days = comp.GetTicksUntilTrade() / 60000f;
                tradeStatus = days.ToString("F1") + " 天";
            }
            listing.Label($"{"RavenRace_Status_TradeCool".Translate()}: {tradeStatus}");
            GUI.color = Color.white;
        }

        private static void DrawFavorabilityBar(Listing_Standard listing, WorldComponent_OperatorManager opManager)
        {
            int favor = opManager.zuoYaoFavorability;
            string favorStr = favor > 0 ? $"+{favor}" : favor.ToString();
            int level = opManager.GetCurrentFavorabilityLevel();

            Rect lineRect = listing.GetRect(22f);

            float heartSize = 22f;
            Rect heartRect = new Rect(lineRect.x, lineRect.y, heartSize, heartSize);
            GUI.DrawTexture(heartRect, IconHeart);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(heartRect, level.ToString());
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.Label(new Rect(heartRect.xMax + 5, lineRect.y, lineRect.width - heartSize - 5, lineRect.height), $"{"RavenRace_OperatorFavorability".Translate()}: {favorStr}");

            Rect barRect = listing.GetRect(12f);
            Widgets.DrawBoxSolid(barRect, new Color(0.1f, 0.1f, 0.1f));
            float fillPct = (favor - WorldComponent_OperatorManager.MinFavorability) / (float)(WorldComponent_OperatorManager.MaxFavorability - WorldComponent_OperatorManager.MinFavorability);
            fillPct = Mathf.Clamp01(fillPct);
            Rect fillRect = barRect;
            fillRect.width *= fillPct;
            Widgets.DrawBoxSolid(fillRect, new Color(1f, 0.41f, 0.71f));
            Widgets.DrawBox(barRect);
        }

        private static void DrawExpressionSwitchButton(Rect portraitFrame, WorldComponent_OperatorManager opManager, Thing radio)
        {
            Rect buttonRect = new Rect(portraitFrame.xMax - 32, portraitFrame.y + 5, 28, 28);
            TooltipHandler.TipRegion(buttonRect, "切换接线员表情套装");

            if (Widgets.ButtonImage(buttonRect, IconSettings))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                int maxLevel = opManager.GetCurrentFavorabilityLevel();

                for (int i = 0; i <= maxLevel; i++)
                {
                    int level = i;
                    string label = $"表情套装: 等级 {level}";
                    if (level == opManager.activeExpressionLevel) label += " (当前)";

                    options.Add(new FloatMenuOption(label, () =>
                    {
                        opManager.activeExpressionLevel = level;
                        Messages.Message($"表情套装已切换为等级 {level}。", MessageTypeDefOf.NeutralEvent);
                        if (Find.WindowStack.IsOpen<Dialog_FusangComm>())
                        {
                            Find.WindowStack.TryRemove(typeof(Dialog_FusangComm), false);
                            Find.WindowStack.Add(new Dialog_FusangComm(radio));
                        }
                    }));
                }
                if (options.Any()) Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public static void DrawResourceBar(Rect rect, Texture2D iconSupplies, Texture2D iconMilitary, Texture2D iconIntel, Texture2D iconInfluence)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);
            float widthPerItem = rect.width / 4f;
            DrawResourceItem(new Rect(rect.x, rect.y, widthPerItem, rect.height), iconSupplies, "RavenRace_Res_Supplies".Translate(), FusangResourceManager.GetAmount(FusangResourceType.Resources));
            DrawResourceItem(new Rect(rect.x + widthPerItem, rect.y, widthPerItem, rect.height), iconMilitary, "RavenRace_Res_Military".Translate(), FusangResourceManager.GetAmount(FusangResourceType.Military));
            DrawResourceItem(new Rect(rect.x + widthPerItem * 2, rect.y, widthPerItem, rect.height), iconIntel, "RavenRace_Res_Intel".Translate(), FusangResourceManager.GetAmount(FusangResourceType.Intel));
            DrawResourceItem(new Rect(rect.x + widthPerItem * 3, rect.y, widthPerItem, rect.height), iconInfluence, "RavenRace_Res_Influence".Translate(), FusangResourceManager.GetAmount(FusangResourceType.Influence));
        }

        private static void DrawResourceItem(Rect rect, Texture2D icon, string label, int value)
        {
            if (rect.x > 15)
            {
                GUI.color = FusangUIStyle.BorderColor;
                Widgets.DrawLineVertical(rect.x, rect.y + 5, rect.height - 10);
                GUI.color = Color.white;
            }
            Rect inner = rect.ContractedBy(6);
            float iconSize = 32f;
            Rect iconRect = new Rect(inner.x, inner.y + (inner.height - iconSize) / 2, iconSize, iconSize);
            GUI.DrawTexture(iconRect, icon);
            float textLeft = iconRect.xMax + 8f;
            float textWidth = inner.width - iconSize - 8f;
            Rect labelRect = new Rect(textLeft, inner.y, textWidth, inner.height / 2);
            Rect valRect = new Rect(textLeft, inner.y + inner.height / 2, textWidth, inner.height / 2);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(labelRect, label);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(valRect, value.ToString());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}