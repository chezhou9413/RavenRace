using RavenRace.Features.Espionage.UI;
using RavenRace.Features.FusangOrganization.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace
{
    public class Dialog_FusangMissionBoard : FusangWindowBase
    {
        public override Vector2 InitialSize => new Vector2(950f, 650f);
        protected override float Margin => 0f;

        private string selectedMissionKey = null;
        private Thing radio;

        public Dialog_FusangMissionBoard(Thing radio) : base() // [修改]
        {
            this.radio = radio;
        }

        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            // 标题
            Text.Font = GameFont.Medium;
            Color oldColor = GUI.color;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(20, 15, inRect.width, 35), "RavenRace_MissionBoard_Title".Translate());
            GUI.color = oldColor;
            Text.Font = GameFont.Small;

            GUI.color = FusangUIStyle.BorderColor;
            Widgets.DrawLineHorizontal(15, 50, inRect.width - 30);
            GUI.color = Color.white;

            // [修改] 布局调整：现在只显示左侧的“特殊协议”面板，或者居中显示
            // 既然删除了右侧的间谍入口（因为主界面已经有了），我们可以把特殊协议面板居中放大

            float panelWidth = 600f;
            Rect centerPanel = new Rect((inRect.width - panelWidth) / 2f, 80f, panelWidth, inRect.height - 160f);

            DrawSpecialOperations(centerPanel);

            // 底部返回按钮
            Rect bottomRect = new Rect(15, inRect.height - 50, inRect.width - 30, 40);
            if (FusangUIStyle.DrawButton(new Rect(bottomRect.xMax - 160, bottomRect.y, 160, 35), "Back".Translate()))
            {
                Close();
                Find.WindowStack.Add(new Dialog_FusangComm(radio));
            }
        }

        private void DrawSpecialOperations(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(20)); // 加大边距

            GUI.color = FusangUIStyle.MainColor_Gold;
            Text.Font = GameFont.Medium;
            listing.Label(":: 特殊协议 ::");
            Text.Font = GameFont.Small;
            listing.GapLine();
            GUI.color = Color.white;
            listing.Gap(10);

            // 1. 代孕任务
            if (DrawMissionButton(listing, "RavenRace_Mission_Surrogate".Translate(), selectedMissionKey == "Surrogate", true))
            {
                selectedMissionKey = "Surrogate";
                Find.WindowStack.Add(new Dialog_Mission_Surrogate(radio));
                Close();
            }
            listing.Label("通过植入纯血灵卵，为组织繁衍后代。", -1f, new TipSignal("需要消耗影响力"));

            listing.Gap(15f);

            // 2. 余烬之血任务
            if (DrawMissionButton(listing, "【代号：余烬】崇高奉献", selectedMissionKey == "Ember", true))
            {
                selectedMissionKey = "Ember";
                Find.WindowStack.Add(new Dialog_Mission_EmberSacrifice(radio));
                Close();
            }
            listing.Label("请求组织提供一份余烬之血。", -1f, new TipSignal("需要消耗情报与影响力"));

            listing.End();
        }

        private bool DrawMissionButton(Listing_Standard listing, string label, bool selected, bool active = true)
        {
            Rect rect = listing.GetRect(40f); // 稍微加大按钮高度
            if (selected)
            {
                Widgets.DrawBoxSolid(rect, new Color(1f, 0.8f, 0.3f, 0.1f));
            }
            bool clicked = FusangUIStyle.DrawButton(rect, label, active);
            listing.Gap(5f);
            return clicked;
        }
    }
}