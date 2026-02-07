using System.Linq;
using RavenRace.Features.FusangOrganization.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Features.Espionage.UI
{
    [StaticConstructorOnStartup]
    public class Dialog_Espionage_Menu : FusangWindowBase
    {
        public override Vector2 InitialSize => new Vector2(1000f, 700f); // [统一尺寸]
        protected override float Margin => 0f;

        private static readonly Texture2D CloseXSmall = ContentFinder<Texture2D>.Get("UI/Widgets/CloseXSmall", true);
        private static readonly Texture2D IconBack = ContentFinder<Texture2D>.Get("UI/Widgets/BackArrow", false) ?? CloseXSmall;

        private Thing radio; // [新增] 记录来源电台

        public Dialog_Espionage_Menu(Thing radio = null) : base()
        {
            this.radio = radio;

        }


        public override void WindowUpdate()
        {
            base.WindowUpdate();

            // [修复] 时间控制
            if (Event.current.type == EventType.KeyDown)
            {
                if (KeyBindingDefOf.TogglePause.KeyDownEvent) { Find.TickManager.TogglePaused(); Event.current.Use(); }
                if (KeyBindingDefOf.TimeSpeed_Normal.KeyDownEvent) { Find.TickManager.CurTimeSpeed = TimeSpeed.Normal; Event.current.Use(); }
                if (KeyBindingDefOf.TimeSpeed_Fast.KeyDownEvent) { Find.TickManager.CurTimeSpeed = TimeSpeed.Fast; Event.current.Use(); }
                if (KeyBindingDefOf.TimeSpeed_Superfast.KeyDownEvent) { Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast; Event.current.Use(); }
            }
        }


        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            Rect titleRect = new Rect(0, 0, inRect.width, 45);
            Widgets.DrawBoxSolid(titleRect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(titleRect, FusangUIStyle.BorderColor);

            // [修复] 确保 radio 不为空，否则无法回退
            // 这里有个逻辑陷阱：如果 radio 在构造函数里是 null，回退就无处可去。
            // 确保 FusangComm_UIPanels 传了 radio。
            if (Widgets.ButtonImage(new Rect(10, 10, 24, 24), IconBack))
            {
                Close();
                // 即使 radio 为空，也应该尝试打开主界面（可能需要重新寻找电台，或者直接报错）
                // 稳妥起见，如果 radio 为空，尝试找一个
                if (radio == null) radio = Find.CurrentMap.listerBuildings.AllBuildingsColonistOfDef(FusangDefOf.Raven_FusangRadio).FirstOrDefault();

                if (radio != null) Find.WindowStack.Add(new Dialog_FusangComm(radio));
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(titleRect, "RavenRace_Espionage_MenuTitle".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // [修复] 右上角关闭按钮：彻底关闭所有
            if (Widgets.ButtonImage(new Rect(titleRect.width - 34, 6, 30, 30), CloseXSmall))
            {
                Close();
            }

            // 按钮布局
            float btnWidth = 300f;
            float btnHeight = 200f;
            float spacing = 60f;
            float startX = (inRect.width - (btnWidth * 2 + spacing)) / 2f;
            float startY = (inRect.height - btnHeight) / 2f;

            // 按钮 1
            Rect btnNet = new Rect(startX, startY, btnWidth, btnHeight);
            DrawBigMenuButton(btnNet,
                "RavenRace_Espionage_CommandNet".Translate(),
                "RavenRace_Espionage_CommandNetDesc".Translate(),
                () =>
                {
                    Find.WindowStack.Add(new Dialog_Mission_Espionage(radio));
                    Close();
                });

            // 按钮 2
            Rect btnManage = new Rect(btnNet.xMax + spacing, startY, btnWidth, btnHeight);
            DrawBigMenuButton(btnManage,
                "RavenRace_Espionage_Management".Translate(),
                "RavenRace_Espionage_ManagementDesc".Translate(),
                () =>
                {
                    Find.WindowStack.Add(new Dialog_Espionage_Overview(radio));
                    Close();
                });
        }


        private void DrawBigMenuButton(Rect rect, string label, string desc, System.Action action)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);
            if (Mouse.IsOver(rect)) Widgets.DrawBoxSolid(rect, new Color(1f, 0.8f, 0.3f, 0.1f));
            if (Widgets.ButtonInvisible(rect)) action?.Invoke();

            Rect inner = rect.ContractedBy(20);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect titleRect = new Rect(inner.x, inner.y + 40, inner.width, 40);
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(titleRect, label);
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Rect descRect = new Rect(inner.x, titleRect.yMax + 20, inner.width, 80);
            Widgets.Label(descRect, desc);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}