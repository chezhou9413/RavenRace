using System.Collections.Generic;
using System.Linq;
using RavenRace.Features.FusangOrganization.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Features.Espionage.UI.Graph
{
    public static class EspionageGraphRenderer
    {
        public static void DrawConnectionsRecursive(OfficialData node, Vector2 offset)
        {
            if (!EspionageGraphLayout.NodePositions.ContainsKey(node)) return;
            Vector2 startPos = EspionageGraphLayout.NodePositions[node] + offset;
            bool isVerticalLayout = (node.rank == OfficialRank.HighCouncil);
            float nodeW = EspionageGraphLayout.NodeWidth;
            float nodeH = EspionageGraphLayout.NodeHeight;
            float vSpace = EspionageGraphLayout.VerticalSpacing;
            Vector2 startPoint = new Vector2(startPos.x + nodeW / 2f, startPos.y + nodeH);

            foreach (var child in node.subordinates)
            {
                if (EspionageGraphLayout.NodePositions.ContainsKey(child))
                {
                    Vector2 endPos = EspionageGraphLayout.NodePositions[child] + offset;
                    Vector2 endPoint;
                    if (isVerticalLayout)
                    {
                        endPoint = new Vector2(endPos.x + nodeW / 2f, endPos.y);
                        Widgets.DrawLine(startPoint, endPoint, EspionageGraphAssets.LineColor, 2f);
                        startPoint = new Vector2(endPos.x + nodeW / 2f, endPos.y + nodeH);
                    }
                    else
                    {
                        endPoint = new Vector2(endPos.x + nodeW / 2f, endPos.y);
                        Vector2 midPoint1 = new Vector2(startPoint.x, startPoint.y + (vSpace / 2f));
                        Vector2 midPoint2 = new Vector2(endPoint.x, midPoint1.y);
                        Widgets.DrawLine(startPoint, midPoint1, EspionageGraphAssets.LineColor, 2f);
                        Widgets.DrawLine(midPoint1, midPoint2, EspionageGraphAssets.LineColor, 2f);
                        Widgets.DrawLine(midPoint2, endPoint, EspionageGraphAssets.LineColor, 2f);
                    }
                    DrawConnectionsRecursive(child, offset);
                }
            }
        }

        public static void DrawNodesRecursive(OfficialData node, Vector2 offset)
        {
            if (!EspionageGraphLayout.NodePositions.ContainsKey(node)) return;
            Vector2 pos = EspionageGraphLayout.NodePositions[node] + offset;
            Rect nodeRect = new Rect(pos.x, pos.y, EspionageGraphLayout.NodeWidth, EspionageGraphLayout.NodeHeight);
            DrawSingleNode(nodeRect, node);
            foreach (var child in node.subordinates) DrawNodesRecursive(child, offset);
        }

        private static void DrawSingleNode(Rect rect, OfficialData node)
        {
            // 1. 绘制背景
            Texture2D bg = node.isKnown ? EspionageGraphAssets.FrameKnown : EspionageGraphAssets.FrameNormal;
            Color borderColor = FusangUIStyle.BorderColor;

            if (node.isTurncoat)
            {
                bg = EspionageGraphAssets.FrameTurncoat;
                borderColor = Color.green;
            }
            else if (node.isDead)
            {
                borderColor = Color.red;
            }

            GUI.DrawTexture(rect, bg);
            FusangUIStyle.DrawBorder(rect, borderColor);

            // 2. 布局计算 (绝对值，不再动态)
            // 节点总大小 160x70
            float padding = 6f;
            float iconSize = 58f; // 正方形头像

            // 头像区域
            Rect iconRect = new Rect(rect.x + padding, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);

            // 文本区域：起点在头像右侧 + padding，宽度为剩余空间 - 右侧padding
            float textX = iconRect.xMax + padding;
            float textW = rect.xMax - textX - padding;

            // 3. 绘制头像
            if (node.isKnown)
            {
                Texture portrait = node.GetPortrait();
                GUI.DrawTexture(iconRect, portrait);

                if (node.isDead)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.6f);
                    Widgets.DrawTextureFitted(iconRect, EspionageGraphAssets.IconUnknown, 0.8f);
                    GUI.color = Color.white;
                }
            }
            else
            {
                GUI.DrawTexture(iconRect, EspionageGraphAssets.IconUnknown, ScaleMode.ScaleToFit);
            }

            // 4. 绘制文本
            Text.Anchor = TextAnchor.MiddleLeft;

            if (node.isKnown)
            {
                // 三行布局，总高度约 60px
                // 第一行：名字 (Small字体)
                float line1Y = rect.y + 6f;
                Rect line1 = new Rect(textX, line1Y, textW, 22f);

                Text.Font = GameFont.Small;
                GUI.color = node.isTurncoat ? EspionageGraphAssets.TurncoatNameColor : EspionageGraphAssets.KnownNameColor;

                string nameLabel = node.Label;
                if (node.rank == OfficialRank.Leader) nameLabel = "★ " + nameLabel;
                if (node.isDead) nameLabel += " (已故)";

                // 强制截断 (重要！)
                if (Text.CalcSize(nameLabel).x > textW) nameLabel = nameLabel.Truncate(textW);
                Widgets.Label(line1, nameLabel);

                // 第二行：职级 (Tiny)
                float line2Y = line1.yMax - 2f; // 紧凑一点
                Rect line2 = new Rect(textX, line2Y, textW, 18f);
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                string rankKey = $"RavenRace_OfficialRank_{node.rank}";
                Widgets.Label(line2, rankKey.Translate());

                // 第三行：状态 (Tiny)
                float line3Y = line2.yMax - 2f;
                Rect line3 = new Rect(textX, line3Y, textW, 18f);
                GUI.color = new Color(0.4f, 0.8f, 1f);
                string info = node.isTurncoat ? "Turncoat" : $"忠诚: {node.loyalty:F0}%";
                Widgets.Label(line3, info);
            }
            else
            {
                // 未知目标：在整个右侧区域垂直居中
                Text.Font = GameFont.Small; // 使用 Small 而不是 Tiny，确保清晰
                GUI.color = Color.gray;
                Rect unknownRect = new Rect(textX, rect.y, textW, rect.height);

                // 关键：强制截断，防止“未知目标”换行
                string unknownLabel = "RavenRace_Espionage_UnknownTarget".Translate();
                if (Text.CalcSize(unknownLabel).x > textW) unknownLabel = unknownLabel.Truncate(textW);

                Widgets.Label(unknownRect, unknownLabel);
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // 5. 点击交互
            if (Widgets.ButtonInvisible(rect))
            {
                Find.WindowStack.Add(new UI.Dialog_OfficialDetails(node));
            }

            // 6. 任务标记
            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            bool isTargeted = comp.activeMissions.Any(m => m.targetOfficial == node);
            if (isTargeted)
            {
                // 在右上角绘制感叹号
                Rect markRect = new Rect(rect.xMax - 20, rect.y - 5, 25, 25);
                Text.Font = GameFont.Medium;
                GUI.color = Color.red;
                Widgets.Label(markRect, "!");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }
    }
}