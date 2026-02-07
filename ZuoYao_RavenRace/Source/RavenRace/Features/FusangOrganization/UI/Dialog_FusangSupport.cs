using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace RavenRace
{
    [StaticConstructorOnStartup]
    public class Dialog_FusangSupport : Window
    {
        public override Vector2 InitialSize => new Vector2(950f, 650f);
        protected override float Margin => 0f;
        private Thing radio;

        // 资源图标
        private static Texture2D iconSupplies;
        private static Texture2D iconMilitary;
        private static Texture2D iconIntel;
        private static Texture2D iconInfluence;

        public Dialog_FusangSupport(Thing radio)
        {
            this.radio = radio;
            this.doCloseX = false;
            this.doCloseButton = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;

            if (iconSupplies == null) iconSupplies = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Supplies", false) ?? BaseContent.BadTex;
            if (iconMilitary == null) iconMilitary = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Military", false) ?? BaseContent.BadTex;
            if (iconIntel == null) iconIntel = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Intel", false) ?? BaseContent.BadTex;
            if (iconInfluence == null) iconInfluence = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Influence", false) ?? BaseContent.BadTex;
        }

        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            // 标题
            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(20, 15, inRect.width, 35), "RavenRace_Support_Title".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Widgets.DrawLineHorizontal(15, 50, inRect.width - 30);

            // 顶部资源栏
            Rect resourceRect = new Rect(30, 60, inRect.width - 60, 50);
            DrawResourceBar(resourceRect);

            // 描述
            Rect descRect = new Rect(30, 120, inRect.width - 60, 40);
            Widgets.Label(descRect, "RavenRace_Support_Desc".Translate());

            // 列表区域
            Rect listRect = new Rect(30, 170, inRect.width - 60, inRect.height - 240);
            DrawSupportOptions(listRect);

            // 返回按钮
            Rect btnRect = new Rect(inRect.width - 180, inRect.height - 50, 160, 40);
            if (FusangUIStyle.DrawButton(btnRect, "Back".Translate()))
            {
                Close();
                Find.WindowStack.Add(new Dialog_FusangComm(radio));
            }
        }

        private void DrawResourceBar(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            float widthPerItem = rect.width / 4f;

            DrawResourceItem(new Rect(rect.x, rect.y, widthPerItem, rect.height), iconSupplies, "RavenRace_Res_Supplies".Translate(), FusangResourceManager.GetAmount(FusangResourceType.Resources));
            DrawResourceItem(new Rect(rect.x + widthPerItem, rect.y, widthPerItem, rect.height), iconMilitary, "RavenRace_Res_Military".Translate(), FusangResourceManager.GetAmount(FusangResourceType.Military));
            DrawResourceItem(new Rect(rect.x + widthPerItem * 2, rect.y, widthPerItem, rect.height), iconIntel, "RavenRace_Res_Intel".Translate(), FusangResourceManager.GetAmount(FusangResourceType.Intel));
            DrawResourceItem(new Rect(rect.x + widthPerItem * 3, rect.y, widthPerItem, rect.height), iconInfluence, "RavenRace_Res_Influence".Translate(), FusangResourceManager.GetAmount(FusangResourceType.Influence));
        }

        private void DrawResourceItem(Rect rect, Texture2D icon, string label, int value)
        {
            if (rect.x > 30)
            {
                Color old = GUI.color;
                GUI.color = FusangUIStyle.BorderColor;
                Widgets.DrawLineVertical(rect.x, rect.y + 5, rect.height - 10);
                GUI.color = old;
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
            Color oldColor = GUI.color;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(labelRect, label);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(valRect, value.ToString());
            GUI.color = oldColor;
        }

        private void DrawSupportOptions(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            // [修复] 增加高度到 95f，给标题留出足够空间
            float itemHeight = 95f;

            DrawDonateItem(listing, 100, 5, "RavenRace_Support_SilverSmall", "RavenRace_Support_SilverSmall_Desc", itemHeight);
            listing.Gap(15);

            DrawDonateItem(listing, 500, 30, "RavenRace_Support_SilverMedium", "RavenRace_Support_SilverMedium_Desc", itemHeight);
            listing.Gap(15);

            DrawDonateItem(listing, 2000, 150, "RavenRace_Support_SilverLarge", "RavenRace_Support_SilverLarge_Desc", itemHeight);

            listing.End();
        }

        private void DrawDonateItem(Listing_Standard listing, int cost, int reward, string labelKey, string descKey, float height)
        {
            string label = labelKey.Translate();
            string desc = descKey.Translate();

            bool canAfford = TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, cost);

            Rect rect = listing.GetRect(height);
            FusangUIStyle.DrawPanel(rect);

            // 按钮 (确保在最上层)
            float btnHeight = 35f;
            float btnWidth = 110f;
            Rect btnRect = new Rect(rect.xMax - btnWidth - 20, rect.y + (height - btnHeight) / 2, btnWidth, btnHeight);

            if (FusangUIStyle.DrawButton(btnRect, "捐赠", canAfford))
            {
                TryDonateSilver(cost, reward);
            }

            // [修复] 调整标题位置和高度，防止截断
            Rect textRect = new Rect(rect.x + 20, rect.y + 8, rect.width - btnWidth - 40, 28);
            GUI.color = FusangUIStyle.MainColor_Gold;
            Text.Font = GameFont.Medium;
            Widgets.Label(textRect, label);

            // [修复] 调整描述位置，使其在标题下方
            Rect descRect = new Rect(rect.x + 20, textRect.yMax + 2, rect.width - btnWidth - 40, 50);
            GUI.color = Color.gray;
            Text.Font = GameFont.Small;
            Widgets.Label(descRect, desc);
            GUI.color = Color.white;
        }

        private void TryDonateSilver(int cost, int rewardAmount)
        {
            if (TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, cost))
            {
                TradeUtility.LaunchSilver(Find.CurrentMap, cost);
                FusangResourceManager.Add(FusangResourceType.Resources, rewardAmount);
                FusangResourceManager.Add(FusangResourceType.Military, rewardAmount);
                FusangResourceManager.Add(FusangResourceType.Intel, rewardAmount);
                FusangResourceManager.Add(FusangResourceType.Influence, rewardAmount);
                SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
                Messages.Message("RavenRace_Donate_Success".Translate(), MessageTypeDefOf.PositiveEvent);
                if (radio != null && radio.Spawned) MoteMaker.ThrowText(radio.DrawPos, radio.Map, "+资源", Color.green);
            }
            else
            {
                Messages.Message("RavenRace_Donate_Fail".Translate(cost), MessageTypeDefOf.RejectInput);
            }
        }
    }
}