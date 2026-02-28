using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Features.Reproduction
{
    /// <summary>
    /// 渡鸦繁衍系统专用工具类。
    /// </summary>
    public static class RavenReproductionUtility
    {
        // 防抖字典：记录每个 Pawn 唯一 ID 最后一次增加交配次数的系统 Tick。
        private static Dictionary<int, int> lastLovinRecordTicks = new Dictionary<int, int>();

        // 防抖冷却时间：1000 Ticks (约等于现实时间 16 秒，游戏时间 40 秒)。
        // 确保一次连续的性行为结束回调中，即使被多次触发，也只记录一次。
        private const int DebounceInterval = 1000;

        /// <summary>
        /// 安全地为目标增加一次交配记录（带防抖拦截）。
        /// 无论外界疯狂调用多少次，在冷却期内只生效一次。
        /// </summary>
        public static void AddLovinCountSafely(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.records == null) return;

            // 安全获取 RecordDef
            RecordDef countDef = DefDatabase<RecordDef>.GetNamedSilentFail("Raven_Record_LovinCount");
            if (countDef == null) return;

            int currentTick = Find.TickManager.TicksGame;
            int pawnId = pawn.thingIDNumber;

            // 检查防抖缓存
            if (lastLovinRecordTicks.TryGetValue(pawnId, out int lastTick))
            {
                // 如果当前时间距离上次记录的时间过短，且时间没有回退（防读档异常），则拦截
                if (currentTick - lastTick < DebounceInterval && currentTick >= lastTick)
                {
                    return;
                }
            }

            // 执行增加记录
            pawn.records.Increment(countDef);

            // 更新缓存时间
            lastLovinRecordTicks[pawnId] = currentTick;
        }

        /// <summary>
        /// [可选] 在游戏加载或主菜单时清理缓存，防止长期挂机导致的内存极微小膨胀
        /// </summary>
        public static void ClearCache()
        {
            lastLovinRecordTicks.Clear();
        }
    }
}