using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Features.MechanicalAngel
{
    /// <summary>
    /// 艾吉斯专属的“淫能”需求。
    /// 继承自原版Need，但拥有完全独立的掉电、充能和UI逻辑。
    /// </summary>
    public class Need_AegisLust : Need
    {
        // 淫能的最大值
        public override float MaxLevel => 100f;

        // 淫能每天的自然消耗量
        public float FallPerDay
        {
            get
            {
                if (pawn.Downed || !pawn.Awake()) return 0f;
                // 如果正在做爱/榨汁，不消耗，反而会恢复
                if (pawn.CurJobDef == RavenDefOf.Raven_Job_AegisLustCharge || pawn.CurJobDef == DefDatabase<JobDef>.GetNamedSilentFail("Raven_Job_AegisRampageCharge")) return -100f; // 返回负数代表正在恢复

                // 根据活动状态决定消耗量
                if (pawn.mindState != null && !pawn.mindState.IsIdle)
                {
                    return 20f; // 活动时消耗更多
                }
                return 5f; // 闲置时消耗较少
            }
        }

        // GUI上的增减箭头
        public override int GUIChangeArrow
        {
            get
            {
                float fallPerDay = FallPerDay;
                if (fallPerDay < 0f) return 1; // 正在恢复
                if (fallPerDay > 0f) return -1; // 正在消耗
                return 0;
            }
        }

        public Need_AegisLust(Pawn pawn) : base(pawn) { }

        /// <summary>
        /// 每150 Ticks调用一次，处理能量变化
        /// </summary>
        public override void NeedInterval()
        {
            if (!IsFrozen)
            {
                // 注意：NeedInterval是150 ticks调用一次，一天有400个这样的间隔
                CurLevel -= FallPerDay / 400f;
            }
        }

        /// <summary>
        /// 自定义绘制UI，实现粉色淫能条
        /// </summary>
        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = int.MaxValue, float customMargin = -1, bool drawArrows = true, bool doTooltip = true, Rect? rectForTooltip = null, bool drawLabel = true)
        {
            // 记录并强制染成粉色
            Color originalColor = GUI.color;
            GUI.color = new Color(1f, 0.41f, 0.7f, 1f);

            base.DrawOnGUI(rect, maxThresholdMarkers, customMargin, drawArrows, doTooltip, rectForTooltip, drawLabel);

            // 恢复颜色
            GUI.color = originalColor;
        }

        /// <summary>
        /// 自定义悬停提示文本
        /// </summary>
        public override string GetTipString()
        {
            return (this.LabelCap + ": " + this.CurLevelPercentage.ToStringPercent()).Colorize(new Color(1f, 0.41f, 0.7f)) + "\n" +
                   this.def.description + "\n\n" +
                   "当前每天流失".Translate() + ": " + (this.FallPerDay / 100f).ToStringPercent();
        }
    }
}