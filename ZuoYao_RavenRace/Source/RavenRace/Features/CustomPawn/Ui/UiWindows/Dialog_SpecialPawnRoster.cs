using RavenRace.Features.CustomPawn.Ui.RaveExtension;
using RavenRace.Features.CustomPawn.Ui.RavrGameComp;
using RavenRace.Features.FusangOrganization.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RavenRace.Features.CustomPawn.Ui.UiWindows
{
    [StaticConstructorOnStartup]
    public class Dialog_SpecialPawnRoster : FusangWindowBase
    {
        // ── 布局常量 ─────────────────────────────────────────────

        private const float TitleBarHeight = 45f;
        private const float TabBarHeight = 36f;
        private const float CardSize = 90f;
        private const float CardSpacing = 10f;
        private const float CardNameHeight = 22f;
        private const float PanelPadding = 12f;
        private const float ScrollbarWidth = 16f;
        private const float CornerMarkSize = 8f;
        private static readonly Texture2D CloseXSmall =
            ContentFinder<Texture2D>.Get("UI/Widgets/CloseXSmall", true);
        private static readonly Texture2D SilhouetteMask =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.02f, 0.04f, 0.10f, 0.72f));

        //解锁遮罩：金色调薄层，为未解锁角色增加神秘感
        private static readonly Texture2D SilhouetteOverlay =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.05f, 0.03f, 0.00f, 0.55f));

        private static readonly Texture2D LockIcon =
            ContentFinder<Texture2D>.Get("UI/Icons/Lock", false);

        // ── 窗口属性（修复：可拖动、不暂停游戏、只有单一关闭按钮） ──

        public override Vector2 InitialSize => new Vector2(900f, 650f);

        protected override float Margin => 0f;

        //可拖动
        public override bool IsDebug => false;
      
        private List<string> raceTabLabels;
        private Dictionary<string, List<PawnKindDef>> raceGroups;
        private int selectedTabIndex = 0;
        private Vector2 scrollPos = Vector2.zero;
        private static readonly Vector2 PortraitSize = new Vector2(184f, 300f);

        public Dialog_SpecialPawnRoster()
        {
            // 可拖动
            draggable = true;
            //让玩家自由操作
            forcePause = false;

            BuildRaceGroups();
        }

        private void BuildRaceGroups()
        {
            raceGroups = new Dictionary<string, List<PawnKindDef>>();
            raceTabLabels = new List<string>();

            List<PawnKindDef> allSpecial = DefDatabase<PawnKindDef>.AllDefs
                .Where(k => k.GetModExtension<RaveCustomPawnUiData>() != null)
                .ToList();

            foreach (PawnKindDef kindDef in allSpecial)
            {
                string raceLabel = kindDef.race != null ? kindDef.race.label : "Unknown";

                if (!raceGroups.ContainsKey(raceLabel))
                {
                    raceGroups[raceLabel] = new List<PawnKindDef>();
                    raceTabLabels.Add(raceLabel);
                }

                raceGroups[raceLabel].Add(kindDef);
            }
            raceTabLabels.Sort(StringComparer.OrdinalIgnoreCase);
            //根据配置的pos进行排序
            foreach (var key in raceGroups.Keys.ToList())
            {
                raceGroups[key].Sort((a, b) =>
                {
                    var extA = a.GetModExtension<RaveCustomPawnUiData>();
                    var extB = b.GetModExtension<RaveCustomPawnUiData>();
                    float posA = extA?.pos ?? 0;
                    float posB = extB?.pos ?? 0;
                    return posA.CompareTo(posB);
                });
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            DrawTitleBar(new Rect(0f, 0f, inRect.width, TitleBarHeight));
            DrawTabBar(new Rect(0f, TitleBarHeight, inRect.width, TabBarHeight));
            DrawContent(new Rect(
                0f,
                TitleBarHeight + TabBarHeight,
                inRect.width,
                inRect.height - TitleBarHeight - TabBarHeight
            ));
        }
        private void DrawTitleBar(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 2f), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 4f, rect.width, 1f), new Color(FusangUIStyle.MainColor_Gold.r,
                                                                                               FusangUIStyle.MainColor_Gold.g,
                                                                                               FusangUIStyle.MainColor_Gold.b, 0.35f));
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), FusangUIStyle.BorderColor);
            DrawDiamondDot(new Vector2(rect.x + 14f, rect.center.y), 4f, FusangUIStyle.MainColor_Gold);

            //标题
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(rect, "特殊角色名册");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            //关闭按钮
            Rect closeRect = new Rect(rect.width - 36f, 7f, 30f, 30f);
            DrawDecoratedButton(closeRect, CloseXSmall, () => Close());
        }

        //种族分类标签栏

        private void DrawTabBar(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.06f, 0.06f, 0.08f));
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), FusangUIStyle.BorderColor);
            if (raceTabLabels.Count == 0) return;
            float tabWidth = rect.width / raceTabLabels.Count;
            for (int i = 0; i < raceTabLabels.Count; i++)
            {
                Rect tabRect = new Rect(rect.x + i * tabWidth, rect.y, tabWidth, rect.height);
                bool isSelected = (i == selectedTabIndex);
                if (isSelected)
                {
                    Widgets.DrawBoxSolid(tabRect, new Color(1f, 0.8f, 0.3f, 0.10f));
                    Widgets.DrawBoxSolid(new Rect(tabRect.x + 4f, tabRect.yMax - 3f, tabRect.width - 8f, 3f),
                                         FusangUIStyle.MainColor_Gold);
                    Widgets.DrawBoxSolid(new Rect(tabRect.x, tabRect.y, 1f, tabRect.height),
                                         new Color(FusangUIStyle.MainColor_Gold.r,
                                                   FusangUIStyle.MainColor_Gold.g,
                                                   FusangUIStyle.MainColor_Gold.b, 0.4f));
                    Widgets.DrawBoxSolid(new Rect(tabRect.xMax - 1f, tabRect.y, 1f, tabRect.height),
                                         new Color(FusangUIStyle.MainColor_Gold.r,
                                                   FusangUIStyle.MainColor_Gold.g,
                                                   FusangUIStyle.MainColor_Gold.b, 0.4f));
                }
                else if (Mouse.IsOver(tabRect))
                {
                    Widgets.DrawBoxSolid(tabRect, new Color(1f, 0.8f, 0.3f, 0.05f));
                }

                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = isSelected ? FusangUIStyle.MainColor_Gold : new Color(0.65f, 0.65f, 0.65f);
                Widgets.Label(tabRect, raceTabLabels[i]);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(tabRect) && !isSelected)
                {
                    selectedTabIndex = i;
                    scrollPos = Vector2.zero;
                }
            }
        }

        //卡片区域

        private void DrawContent(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.07f, 0.07f, 0.09f));

            if (raceTabLabels.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "暂无特殊角色");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            string currentRace = raceTabLabels[selectedTabIndex];
            List<PawnKindDef> kinds = raceGroups[currentRace];
            float innerWidth = rect.width - PanelPadding * 2f - ScrollbarWidth;
            int cardsPerRow = Mathf.Max(1, Mathf.FloorToInt(innerWidth / (CardSize + CardSpacing)));
            int rowCount = Mathf.CeilToInt((float)kinds.Count / cardsPerRow);
            float totalHeight = rowCount * (CardSize + CardNameHeight + CardSpacing) + PanelPadding;
            Rect innerRect = rect.ContractedBy(PanelPadding);
            Rect viewRect = new Rect(0f, 0f, innerRect.width - ScrollbarWidth, totalHeight);
            Widgets.BeginScrollView(innerRect, ref scrollPos, viewRect);
            try
            {
                for (int i = 0; i < kinds.Count; i++)
                {
                    int col = i % cardsPerRow;
                    int row = i / cardsPerRow;
                    float x = col * (CardSize + CardSpacing);
                    float y = row * (CardSize + CardNameHeight + CardSpacing);

                    DrawPawnCard(new Rect(x, y, CardSize, CardSize + CardNameHeight), kinds[i]);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        //单张卡片

        private void DrawPawnCard(Rect cardRect, PawnKindDef kindDef)
        {
            GameComponent_SpecialPawnUnlocks comp = GameComponent_SpecialPawnUnlocks.Instance;
            bool isUnlocked = comp != null && comp.IsUnlocked(kindDef);

            Rect portraitRect = new Rect(cardRect.x, cardRect.y, CardSize, CardSize);
            Rect nameRect = new Rect(cardRect.x, cardRect.y + CardSize, CardSize, CardNameHeight);

            bool isHovered = Mouse.IsOver(cardRect);

            //卡片背景
            Widgets.DrawBoxSolid(portraitRect, new Color(0.10f, 0.10f, 0.13f));
            Color borderColor = isUnlocked
                ? (isHovered ? FusangUIStyle.MainColor_Gold : new Color(FusangUIStyle.MainColor_Gold.r,
                                                                         FusangUIStyle.MainColor_Gold.g,
                                                                         FusangUIStyle.MainColor_Gold.b, 0.55f))
                : new Color(0.3f, 0.3f, 0.35f, 0.8f);

            DrawDecoratedBorder(portraitRect, borderColor, isUnlocked && isHovered);
            //悬停内发光
            if (isHovered && isUnlocked)
            {
                Widgets.DrawBoxSolid(portraitRect.ContractedBy(1f),
                                     new Color(FusangUIStyle.MainColor_Gold.r,
                                               FusangUIStyle.MainColor_Gold.g,
                                               FusangUIStyle.MainColor_Gold.b, 0.06f));
            }
            //绘制头像
            DrawPortrait(portraitRect, kindDef, isUnlocked, comp);
            //名字标签
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = isUnlocked ? FusangUIStyle.MainColor_Gold : new Color(0.45f, 0.45f, 0.50f);
            Widgets.Label(nameRect, isUnlocked ? kindDef.LabelCap.ToString() : "???");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // Tooltip
            if (isHovered)
                TooltipHandler.TipRegion(cardRect, isUnlocked ? kindDef.LabelCap.ToString() : "未解锁");

            // 点击
            if (Widgets.ButtonInvisible(cardRect) && isUnlocked)
                OnCardClicked(kindDef, comp);
        }

        //头像绘制

        private void DrawPortrait(Rect rect, PawnKindDef kindDef, bool isUnlocked, GameComponent_SpecialPawnUnlocks comp)
        {
            Pawn previewPawn = comp?.GetPreviewPawn(kindDef);

            if (previewPawn == null)
            {
                Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.12f, 0.15f));
                return;
            }
            RenderTexture portrait = PortraitsCache.Get(
                previewPawn,
                PortraitSize,
                Rot4.South,
                new Vector3(0f, 0f, 0.3f),
                1.28205f,
                supersample: false,
                compensateForUIScale: false,
                renderHeadgear: true,
                renderClothes: true
            );

            GUI.DrawTexture(rect, portrait, ScaleMode.ScaleAndCrop);

            if (!isUnlocked)
            {
                //先画一层颜色叠加让角色偏暗蓝色调
                GUI.DrawTexture(rect, SilhouetteOverlay);

                //再叠加暗化层
                GUI.DrawTexture(rect, SilhouetteMask);

                //锁图标居中
                if (LockIcon != null)
                {
                    float iconSize = 22f;
                    Rect lockRect = new Rect(
                        rect.x + (rect.width - iconSize) / 2f,
                        rect.y + (rect.height - iconSize) / 2f,
                        iconSize, iconSize
                    );
                    GUI.color = new Color(FusangUIStyle.MainColor_Gold.r,
                                          FusangUIStyle.MainColor_Gold.g,
                                          FusangUIStyle.MainColor_Gold.b, 0.75f);
                    GUI.DrawTexture(lockRect, LockIcon);
                    GUI.color = Color.white;
                }
            }
        }

        //点击回调：打开详情子页面

        private void OnCardClicked(PawnKindDef kindDef, GameComponent_SpecialPawnUnlocks comp)
        {
            Find.WindowStack.Add(new Dialog_SpecialPawnDetail(kindDef));
        }

        //带角标的设计感边框 ─────────────────────────

        /// <summary>
        /// 绘制带四角装饰的设计感边框。
        /// 基础 1px 边框 + 四个角的双线角标（L 形）。
        /// </summary>
        private static void DrawDecoratedBorder(Rect rect, Color color, bool bright)
        {
            float alpha = bright ? 1.0f : 1.0f;
            Color main = new Color(color.r, color.g, color.b, color.a * alpha);
            Color dim = new Color(color.r, color.g, color.b, color.a * 0.35f);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), main);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), main);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 1f, rect.height), main);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), main);
            float m = CornerMarkSize;
            float t = 2f; // 角标线宽

            //左上角
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, m, t), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, t, m), FusangUIStyle.MainColor_Gold);

            //右上角
            Widgets.DrawBoxSolid(new Rect(rect.xMax - m, rect.y, m, t), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - t, rect.y, t, m), FusangUIStyle.MainColor_Gold);

            //左下角
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - t, m, t), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - m, t, m), FusangUIStyle.MainColor_Gold);

            //右下角
            Widgets.DrawBoxSolid(new Rect(rect.xMax - m, rect.yMax - t, m, t), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - t, rect.yMax - m, t, m), FusangUIStyle.MainColor_Gold);
        }

        /// <summary>
        /// 绘制带设计感的图标按钮（带角标装饰边框）。
        /// </summary>
        private static void DrawDecoratedButton(Rect rect, Texture2D icon, Action onClick)
        {
            bool hovered = Mouse.IsOver(rect);

            Color bg = hovered
                ? new Color(FusangUIStyle.MainColor_Gold.r,
                             FusangUIStyle.MainColor_Gold.g,
                             FusangUIStyle.MainColor_Gold.b, 0.18f)
                : new Color(0f, 0f, 0f, 0.25f);

            Widgets.DrawBoxSolid(rect, bg);

            // 装饰边框
            Color borderCol = hovered
                ? FusangUIStyle.MainColor_Gold
                : new Color(FusangUIStyle.MainColor_Gold.r,
                             FusangUIStyle.MainColor_Gold.g,
                             FusangUIStyle.MainColor_Gold.b, 0.5f);
            DrawDecoratedBorder(rect, borderCol, hovered);

            // 图标
            float pad = 6f;
            Rect iconR = rect.ContractedBy(pad);
            GUI.color = hovered ? Color.white : new Color(0.85f, 0.85f, 0.85f, 0.9f);
            GUI.DrawTexture(iconR, icon, ScaleMode.ScaleToFit);
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(rect))
                onClick?.Invoke();
        }

        /// <summary>
        /// 绘制装饰性菱形点。
        /// </summary>
        private static void DrawDiamondDot(Vector2 center, float size, Color color)
        {
            GUI.color = color;
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, center);
            Widgets.DrawBoxSolid(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), color);
            GUI.matrix = matrix;
            GUI.color = Color.white;
        }
    }
}