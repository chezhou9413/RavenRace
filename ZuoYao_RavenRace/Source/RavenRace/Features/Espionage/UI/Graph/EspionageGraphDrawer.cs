using RavenRace.Features.Espionage.UI.Graph; // 引用新的命名空间
using UnityEngine;
using Verse;

namespace RavenRace.Features.Espionage.UI
{
    /// <summary>
    /// 间谍权力结构图的绘制入口。
    /// 这是一个外观类 (Facade)，协调 Layout 和 Renderer。
    /// </summary>
    public static class EspionageGraphDrawer
    {
        /// <summary>
        /// 重新计算布局 (当切换派系或数据变化时调用)
        /// </summary>
        public static void RecalculateLayout(OfficialData root)
        {
            EspionageGraphLayout.Recalculate(root);
        }

        /// <summary>
        /// 获取图表的完整尺寸 (用于 ScrollView)
        /// </summary>
        public static Vector2 GetGraphSize()
        {
            return EspionageGraphLayout.GraphSize;
        }

        /// <summary>
        /// 在指定区域内绘制图表
        /// </summary>
        /// <param name="canvasRect">ScrollView 的可视区域</param>
        /// <param name="scrollPosition">当前滚动位置 (暂时没用到，因为是在 Group 内部画)</param>
        /// <param name="root">根节点</param>
        public static void DrawGraph(Rect canvasRect, Vector2 scrollPosition, OfficialData root)
        {
            if (root == null || EspionageGraphLayout.NodePositions.Count == 0) return;

            // 1. 确定内容区域大小
            Vector2 graphSize = EspionageGraphLayout.GraphSize;
            float contentWidth = Mathf.Max(canvasRect.width, graphSize.x);
            float contentHeight = Mathf.Max(canvasRect.height, graphSize.y);

            // 2. 计算居中偏移
            Vector2 offset = EspionageGraphLayout.CalculateCenterOffset(canvasRect);

            // 3. 开始绘制组
            GUI.BeginGroup(new Rect(0, 0, contentWidth, contentHeight));
            try
            {
                EspionageGraphRenderer.DrawConnectionsRecursive(root, offset);
                EspionageGraphRenderer.DrawNodesRecursive(root, offset);
            }
            finally
            {
                GUI.EndGroup();
            }
        }
    }
}