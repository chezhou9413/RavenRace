using Verse;
using RimWorld;

namespace RavenRace
{
    /// <summary>
    /// 所有陷阱效果的基类。
    /// 具体的陷阱（如踏板、毒气释放）都应该继承这个类。
    /// </summary>
    public abstract class CompTrapEffect : ThingComp
    {
        /// <summary>
        /// 当陷阱被触发时调用
        /// </summary>
        /// <param name="triggerer">触发者 (可能为null)</param>
        public abstract void OnTriggered(Pawn triggerer);

        /// <summary>
        /// 当陷阱重置/重新武装时调用
        /// </summary>
        public virtual void OnRearm() { }
    }
}