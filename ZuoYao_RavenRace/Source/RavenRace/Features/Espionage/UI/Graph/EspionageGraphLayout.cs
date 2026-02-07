using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RavenRace.Features.Espionage.UI.Graph
{
    public static class EspionageGraphLayout
    {
        // [优化] 适中的节点尺寸，平衡信息量与图表密度
        public const float NodeWidth = 160f;
        public const float NodeHeight = 80f;

        public const float HorizontalSpacing = 25f;
        public const float VerticalSpacing = 65f;
        public const float SubordinateVerticalSpacing = 12f;

        private static Dictionary<OfficialData, Vector2> nodePositions = new Dictionary<OfficialData, Vector2>();
        private static Dictionary<OfficialData, float> subtreeWidths = new Dictionary<OfficialData, float>();
        private static float totalGraphWidth = 0f;
        private static float totalGraphHeight = 0f;

        public static Dictionary<OfficialData, Vector2> NodePositions => nodePositions;
        public static Vector2 GraphSize => new Vector2(Mathf.Max(800f, totalGraphWidth), Mathf.Max(600f, totalGraphHeight));

        public static void Recalculate(OfficialData root)
        {
            nodePositions.Clear();
            subtreeWidths.Clear();
            totalGraphWidth = 0f;
            totalGraphHeight = 0f;

            if (root == null) return;

            CalculateSubtreeWidth(root);
            AssignPositions(root, 0f, 0f);
            CalculateBounds();
        }

        private static float CalculateSubtreeWidth(OfficialData node)
        {
            float width = 0f;
            if (node.rank == OfficialRank.HighCouncil && !node.subordinates.NullOrEmpty())
            {
                width = NodeWidth + HorizontalSpacing;
            }
            else if (node.subordinates.NullOrEmpty())
            {
                width = NodeWidth + HorizontalSpacing;
            }
            else
            {
                foreach (var child in node.subordinates)
                {
                    width += CalculateSubtreeWidth(child);
                }
            }
            subtreeWidths[node] = width;
            return width;
        }

        private static void AssignPositions(OfficialData node, float x, float y)
        {
            nodePositions[node] = new Vector2(x, y);
            if (node.subordinates.NullOrEmpty()) return;

            if (node.rank == OfficialRank.HighCouncil)
            {
                float currentY = y + NodeHeight + VerticalSpacing;
                foreach (var child in node.subordinates)
                {
                    AssignPositions(child, x, currentY);
                    currentY += NodeHeight + SubordinateVerticalSpacing;
                }
            }
            else
            {
                float currentX = x - (subtreeWidths[node] / 2f);
                float nextY = y + NodeHeight + VerticalSpacing;
                foreach (var child in node.subordinates)
                {
                    float childWidth = subtreeWidths[child];
                    float childCenterX = currentX + (childWidth / 2f);
                    AssignPositions(child, childCenterX, nextY);
                    currentX += childWidth;
                }
            }
        }

        private static void CalculateBounds()
        {
            if (nodePositions.Count == 0) return;
            float minX = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var pos in nodePositions.Values)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y > maxY) maxY = pos.y;
            }
            totalGraphWidth = (maxX - minX) + NodeWidth + 100f;
            totalGraphHeight = maxY + NodeHeight + 100f;
        }

        public static Vector2 CalculateCenterOffset(Rect canvasRect)
        {
            if (nodePositions.Count == 0) return Vector2.zero;
            float minX = float.MaxValue;
            foreach (var pos in nodePositions.Values) if (pos.x < minX) minX = pos.x;
            float offsetX = 50f - minX;
            if (totalGraphWidth < canvasRect.width)
            {
                offsetX += (canvasRect.width - totalGraphWidth) / 2f;
            }
            return new Vector2(offsetX, 50f);
        }
    }
}