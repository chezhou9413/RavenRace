using UnityEngine;
using Verse;

namespace RavenRace
{
    [StaticConstructorOnStartup]
    public static class FusangUIStyle
    {
        // 调亮了颜色，增加对比度
        public static readonly Color MainColor_Gold = new Color(1.0f, 0.8f, 0.3f);
        public static readonly Color MainColor_DarkGold = new Color(0.6f, 0.45f, 0.15f);
        public static readonly Color MainColor_Black = new Color(0.05f, 0.05f, 0.05f);
        public static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.12f);
        public static readonly Color BorderColor = new Color(0.4f, 0.35f, 0.2f);
        public static readonly Color TextColor = new Color(0.95f, 0.9f, 0.8f);

        // 补充缺失的颜色定义
        public static readonly Color TerminalGray = new Color(0.4f, 0.4f, 0.4f);

        public static void DrawBorder(Rect rect, Color color, int thickness = 1)
        {
            Color old = GUI.color;
            GUI.color = color;
            Widgets.DrawBox(rect, thickness);
            GUI.color = old;
        }

        public static void DrawBackground(Rect rect)
        {
            // 绘制背景
            Widgets.DrawBoxSolid(rect, MainColor_Black);
            // 绘制外边框
            DrawBorder(rect, BorderColor, 2);
        }

        public static void DrawPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, PanelColor);
            DrawBorder(rect, BorderColor, 1);
        }

        public static bool DrawButton(Rect rect, string label, bool active = true)
        {
            Color oldColor = GUI.color;

            // 背景 (禁用时更暗)
            Color bgColor = active ? PanelColor : new Color(0.08f, 0.08f, 0.08f);
            Widgets.DrawBoxSolid(rect, bgColor);

            // 边框 (激活=金，禁用=暗灰)
            Color borderColor = active ? MainColor_Gold : new Color(0.3f, 0.3f, 0.3f);
            if (!active) GUI.color = Color.gray;

            DrawBorder(rect, borderColor);

            // 高亮 (仅激活时)
            if (active && Mouse.IsOver(rect))
            {
                Widgets.DrawBoxSolid(rect, new Color(1.0f, 0.8f, 0.3f, 0.15f));
            }

            // 文本 (自动阴影)
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;

            if (!active)
            {
                GUI.color = Color.gray;
                Widgets.Label(rect, label);
            }
            else
            {
                // 阴影
                Rect shadowRect = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
                GUI.color = new Color(0, 0, 0, 0.8f);
                Widgets.Label(shadowRect, label);

                // 正文
                GUI.color = TextColor;
                Widgets.Label(rect, label);
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = oldColor;

            // 点击检测
            return active && Widgets.ButtonInvisible(rect);
        }
    }
}