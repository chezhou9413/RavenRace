using System.Collections.Generic;
using Verse;

namespace RavenRace.Features.DefenseSystem
{
    /// <summary>
    /// 全局诱饵缓存
    /// 用于优化战斗AI补丁，避免在没有假人时进行无效的类型检查。
    /// </summary>
    public static class RavenDecoyCache
    {
        // 使用 HashSet 提供 O(1) 复杂度的查找
        private static readonly HashSet<int> ActiveDecoyIds = new HashSet<int>();

        /// <summary>
        /// 当前是否有活跃的诱饵
        /// </summary>
        public static bool HasActiveDecoys => ActiveDecoyIds.Count > 0;

        /// <summary>
        /// 注册诱饵
        /// </summary>
        public static void Register(Thing decoy)
        {
            if (decoy != null) ActiveDecoyIds.Add(decoy.thingIDNumber);
        }

        /// <summary>
        /// 注销诱饵
        /// </summary>
        public static void Deregister(Thing decoy)
        {
            if (decoy != null) ActiveDecoyIds.Remove(decoy.thingIDNumber);
        }

        /// <summary>
        /// 检查指定 Thing 是否为已注册的诱饵 (极速)
        /// </summary>
        public static bool IsRegisteredDecoy(Thing t)
        {
            return t != null && ActiveDecoyIds.Contains(t.thingIDNumber);
        }

        /// <summary>
        /// 游戏重新加载时清理缓存
        /// </summary>
        public static void Clear()
        {
            ActiveDecoyIds.Clear();
        }
    }
}