using RavenRace.Features.CustomPawn.Ui.RaveExtension;
using RavenRace.Features.CustomPawn.Ui.RavrGameComp;
using RavenRace.Features.FusangOrganization.UI;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace RavenRace.Features.CustomPawn.Ui.UiWindows
{
    [StaticConstructorOnStartup]
    public class Dialog_SpecialPawnDetail : FusangWindowBase
    {
        //布局常量
        private const float TitleBarHeight = 45f;
        private const float PanelPadding = 16f;
        private const float ButtonHeight = 38f;
        private const float ButtonWidth = 140f;
        private const float ButtonSpacing = 12f;
        private const float CornerMarkSize = 8f;
        private const float PortraitPanelRatio = 0.33f;

        //全身像渲染参数
        private static readonly Vector2 PortraitRenderSize = new Vector2(512f, 768f);

        //纹理
        private static readonly Texture2D CloseXSmall =
            ContentFinder<Texture2D>.Get("UI/Widgets/CloseXSmall", true);

        private static readonly Texture2D IconSummon =
            ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/MentalStateAggro", false) ?? CloseXSmall;

        private static readonly Texture2D IconReclaim =
            ContentFinder<Texture2D>.Get("UI/Icons/ColonistBar/Sleeping", false) ?? CloseXSmall;

        //数据
        private readonly PawnKindDef kindDef;
        private readonly RaveCustomPawnUiData uiData;
        private readonly GameComponent_SpecialPawnUnlocks comp;

        //介绍文本滚动
        private Vector2 descScrollPos = Vector2.zero;

        //窗口属性

        public override Vector2 InitialSize => new Vector2(860f, 560f);
        protected override float Margin => 0f;
        public Dialog_SpecialPawnDetail(PawnKindDef kindDef)
        {
            this.kindDef = kindDef;
            this.uiData = kindDef.GetModExtension<RaveCustomPawnUiData>();
            this.comp = GameComponent_SpecialPawnUnlocks.Instance;

            draggable = true;
            forcePause = false;
        }
        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);
            //标题栏
            DrawTitleBar(new Rect(0f, 0f, inRect.width, TitleBarHeight));
            //内容区
            Rect contentRect = new Rect(
                0f,
                TitleBarHeight,
                inRect.width,
                inRect.height - TitleBarHeight
            );
            DrawContent(contentRect);
        }

        //标题栏
        private void DrawTitleBar(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            //顶部金色双线
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 2f), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y + 4f, rect.width, 1f),
                new Color(FusangUIStyle.MainColor_Gold.r, FusangUIStyle.MainColor_Gold.g, FusangUIStyle.MainColor_Gold.b, 0.35f));
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), FusangUIStyle.BorderColor);
            //菱形装饰
            DrawDiamondDot(new Vector2(rect.x + 14f, rect.center.y), 4f, FusangUIStyle.MainColor_Gold);
            //标题：角色名
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(rect, kindDef.LabelCap);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            //关闭按钮
            DrawDecoratedButton(new Rect(rect.width - 36f, 7f, 30f, 30f), CloseXSmall, () => Close());
        }

        //内容区：左侧全身像+右侧信息

        private void DrawContent(Rect rect)
        {
            float portraitPanelW = rect.width * PortraitPanelRatio;
            float infoPanelW = rect.width - portraitPanelW;

            Rect portraitPanel = new Rect(rect.x, rect.y, portraitPanelW, rect.height);
            Rect infoPanel = new Rect(rect.x + portraitPanelW, rect.y, infoPanelW, rect.height);

            DrawPortraitPanel(portraitPanel);
            DrawInfoPanel(infoPanel);
        }
  
        private void DrawPortraitPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.07f, 0.07f, 0.09f));
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), FusangUIStyle.BorderColor);
            Pawn previewPawn = comp?.GetPreviewPawn(kindDef);
            if (previewPawn == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "无预览");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }
            RenderTexture portrait = PortraitsCache.Get(
                previewPawn,
                PortraitRenderSize,
                Rot4.South,
                new Vector3(0f, 0f, 0f),   //不偏移，显示全身
                1.0f,                       //标准缩放，看到完整身体
                supersample: false,
                compensateForUIScale: false,
                renderHeadgear: true,
                renderClothes: true
            );
            //保持比例，居中绘制（padding缩小让图像更大）
            float padding = 6f;
            float availW = rect.width - padding * 2f;
            float availH = rect.height - padding * 2f;
            float texAspect = (float)PortraitRenderSize.x / PortraitRenderSize.y; // 宽/高
            float drawH = availH;
            float drawW = drawH * texAspect;
            if (drawW > availW)
            {
                drawW = availW;
                drawH = drawW / texAspect;
            }
            float drawX = rect.x + (rect.width - drawW) / 2f;
            float drawY = rect.y + (rect.height - drawH) / 2f;
            Rect drawRect = new Rect(drawX, drawY, drawW, drawH);
            //边框
            DrawDecoratedBorder(drawRect.ExpandedBy(2f), FusangUIStyle.MainColor_Gold, false);
            GUI.DrawTexture(drawRect, portrait, ScaleMode.ScaleToFit);
        }

        //信息面板
        private void DrawInfoPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.10f));

            float p = PanelPadding;
            //按钮区高度（底部固定）
            float buttonAreaH = ButtonHeight + p * 2f;
            //介绍文本区
            Rect descAreaOuter = new Rect(rect.x + p, rect.y + p,
                rect.width - p * 2f, rect.height - p * 2f - buttonAreaH);
            DrawDescriptionArea(descAreaOuter);
            //分隔线
            float sepY = rect.yMax - buttonAreaH - 1f;
            Widgets.DrawBoxSolid(new Rect(rect.x, sepY, rect.width, 1f), FusangUIStyle.BorderColor);
            //按钮区
            Rect buttonArea = new Rect(rect.x, rect.yMax - buttonAreaH, rect.width, buttonAreaH);
            DrawButtonArea(buttonArea);
        }

        //介绍文本滚动区
        private void DrawDescriptionArea(Rect outerRect)
        {
            // 标题
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(0.7f, 0.7f, 0.75f);

            float textWidth = outerRect.width - 20f; //留滚动条宽度
            float calcY = 8f;
            calcY += 22f;          
            calcY += 34f;           
            calcY += 22f;           
            string descText2 = BuildDescText();
            Text.Font = GameFont.Small;
            float descH = Text.CalcHeight(descText2, textWidth);
            calcY += descH + 8f;
            if (kindDef.race != null)
            {
                calcY += 22f;      
                string raceInfo2 = $"种族：{kindDef.race.LabelCap}";
                calcY += Text.CalcHeight(raceInfo2, textWidth) + 4f;
            }
            calcY += 16f;         
            float viewHeight = Mathf.Max(calcY, outerRect.height);
            Rect viewRect = new Rect(0f, 0f, textWidth, viewHeight);
            Widgets.BeginScrollView(outerRect, ref descScrollPos, viewRect);
            float y = 8f;
            //名称
            DrawSectionLabel(ref y, textWidth, "[ 特殊角色 ]");
            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(new Rect(0f, y, textWidth, 32f), kindDef.LabelCap);
            y += 34f;
            Text.Font = GameFont.Small;
            //描述
            DrawSectionLabel(ref y, textWidth, "[ 背景介绍 ]");
            GUI.color = new Color(0.82f, 0.82f, 0.86f);
            float dh = Text.CalcHeight(descText2, textWidth);
            Widgets.Label(new Rect(0f, y, textWidth, dh), descText2);
            y += dh + 8f;
            //种族信息
            if (kindDef.race != null)
            {
                DrawSectionLabel(ref y, textWidth, "[ 种族信息 ]");
                GUI.color = new Color(0.7f, 0.7f, 0.75f);
                string raceInfo = $"种族：{kindDef.race.LabelCap}";
                float rh = Text.CalcHeight(raceInfo, textWidth);
                Widgets.Label(new Rect(0f, y, textWidth, rh), raceInfo);
                y += rh + 4f;
            }
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            Widgets.EndScrollView();
        }

        private void DrawSectionLabel(ref float y, float width, string label)
        {
            GUI.color = new Color(FusangUIStyle.MainColor_Gold.r,
                                   FusangUIStyle.MainColor_Gold.g,
                                   FusangUIStyle.MainColor_Gold.b, 0.6f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(0f, y, width, 18f), label);
            // 下划线
            Widgets.DrawBoxSolid(new Rect(0f, y + 16f, width, 1f),
                new Color(FusangUIStyle.MainColor_Gold.r,
                           FusangUIStyle.MainColor_Gold.g,
                           FusangUIStyle.MainColor_Gold.b, 0.25f));
            y += 22f;
            Text.Font = GameFont.Small;
        }

        private string BuildDescText()
        {
            if (uiData != null && !uiData.CustomPawnUiDes.NullOrEmpty())
                return uiData.CustomPawnUiDes;
            if (!kindDef.description.NullOrEmpty())
                return kindDef.description;

            return "该角色暂无详细介绍。";
        }

        // 按钮区

        private void DrawButtonArea(Rect rect)
        {
            bool isSpawned = comp != null && comp.IsSpawned(kindDef);

            float totalW = ButtonWidth * 2f + ButtonSpacing;
            float startX = rect.x + (rect.width - totalW) / 2f;
            float startY = rect.y + (rect.height - ButtonHeight) / 2f;

            //召唤按钮
            bool canSummon = comp != null && !isSpawned;
            Rect summonRect = new Rect(startX, startY, ButtonWidth, ButtonHeight);
            DrawActionButton(summonRect, "召　唤", FusangUIStyle.MainColor_Gold, canSummon, () =>
            {
                //关闭两级
                Close();
                Find.WindowStack.TryRemove(typeof(Dialog_SpecialPawnRoster), doCloseSound: false);
                StartSummonTargeting();
            });
            //收回按钮
            bool canReclaim = comp != null && isSpawned;
            Rect reclaimRect = new Rect(startX + ButtonWidth + ButtonSpacing, startY, ButtonWidth, ButtonHeight);
            DrawActionButton(reclaimRect, "收　回", new Color(0.6f, 0.75f, 1f), canReclaim, () =>
            {
                comp.ReclaimPawn(kindDef,uiData);
            });
        }

        //打开地点选择器

        private void StartSummonTargeting()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;
            TargetingParameters tp = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = false,
                canTargetBuildings = false,
                validator = (TargetInfo t) =>
                    t.Cell.IsValid && t.Cell.Standable(map)
            };
            PawnKindDef capturedKind = kindDef;
            Find.Targeter.BeginTargeting(
                tp,
                action: (LocalTargetInfo target) =>
                {
                    if (!target.Cell.IsValid || !target.Cell.Standable(map))
                    {
                        Messages.Message("无法在此处召唤：地点不可站立。",
                            MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                    Pawn result = comp.GivePawnToPlayer(capturedKind, target.Cell, map,uiData);
                    if (result != null)
                    {
                        Messages.Message($"{result.LabelShortCap} 已在指定位置召唤。",
                            MessageTypeDefOf.PositiveEvent, false);
                    }
                    else
                    {
                        Messages.Message("召唤失败，该角色可能已在场上或已死亡。",
                            MessageTypeDefOf.RejectInput, false);
                    }
                },
                caster: null,
                actionWhenFinished: null,
                mouseAttachment: null,
                requiresCastedSelected: false
            );
        }

        // 工具方法,下面这些完全是ai写的，反正实现了，我没具体看
        private static void DrawActionButton(Rect rect, string label, Color accentColor,
            bool enabled, Action onClick)
        {
            bool hovered = Mouse.IsOver(rect) && enabled;
            //背景
            Color bg = !enabled
                ? new Color(0.12f, 0.12f, 0.14f, 0.6f)
                : hovered
                    ? new Color(accentColor.r, accentColor.g, accentColor.b, 0.22f)
                    : new Color(accentColor.r, accentColor.g, accentColor.b, 0.10f);
            Widgets.DrawBoxSolid(rect, bg);
            //边框
            Color borderCol = !enabled
                ? new Color(0.3f, 0.3f, 0.33f, 0.5f)
                : hovered ? accentColor
                          : new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f);
            DrawDecoratedBorder(rect, borderCol, hovered);
            // 文字
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = !enabled ? new Color(0.4f, 0.4f, 0.4f)
                                   : hovered ? Color.white : accentColor;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            if (enabled && Widgets.ButtonInvisible(rect))
                onClick?.Invoke();
        }

        private static void DrawDecoratedBorder(Rect rect, Color color, bool bright)
        {
            Color main = new Color(color.r, color.g, color.b, color.a);

            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), main);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), main);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 1f, rect.height), main);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), main);

            float m = CornerMarkSize;
            float t = 2f;

            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, m, t), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, t, m), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - m, rect.y, m, t), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - t, rect.y, t, m), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - t, m, t), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - m, t, m), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - m, rect.yMax - t, m, t), FusangUIStyle.MainColor_Gold);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - t, rect.yMax - m, t, m), FusangUIStyle.MainColor_Gold);
        }

        private static void DrawDecoratedButton(Rect rect, Texture2D icon, Action onClick)
        {
            bool hovered = Mouse.IsOver(rect);
            Widgets.DrawBoxSolid(rect,
                hovered ? new Color(FusangUIStyle.MainColor_Gold.r,
                                     FusangUIStyle.MainColor_Gold.g,
                                     FusangUIStyle.MainColor_Gold.b, 0.18f)
                        : new Color(0f, 0f, 0f, 0.25f));
            DrawDecoratedBorder(rect,
                hovered ? FusangUIStyle.MainColor_Gold
                        : new Color(FusangUIStyle.MainColor_Gold.r,
                                     FusangUIStyle.MainColor_Gold.g,
                                     FusangUIStyle.MainColor_Gold.b, 0.5f), hovered);
            GUI.color = hovered ? Color.white : new Color(0.85f, 0.85f, 0.85f, 0.9f);
            GUI.DrawTexture(rect.ContractedBy(6f), icon, ScaleMode.ScaleToFit);
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(rect)) onClick?.Invoke();
        }

        private static void DrawDiamondDot(Vector2 center, float size, Color color)
        {
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, center);
            Widgets.DrawBoxSolid(
                new Rect(center.x - size / 2f, center.y - size / 2f, size, size), color);
            GUI.matrix = matrix;
        }
    }
}