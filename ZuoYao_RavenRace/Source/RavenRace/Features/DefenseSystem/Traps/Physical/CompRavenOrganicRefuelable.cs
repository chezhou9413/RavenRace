using RimWorld;
using Verse;

namespace RavenRace.Features.DefenseSystem.Traps
{
    /// <summary>
    /// 渡鸦有机物装填组件
    /// 继承自原版 CompRefuelable，但在 Harmony 补丁中被拦截，
    /// 将装填逻辑改为“基于营养值”而不是“基于物品数量”。
    /// </summary>
    public class CompRavenOrganicRefuelable : CompRefuelable
    {
        // 这是一个标记类，具体逻辑在 Harmony/Patch_RavenRefuelable.cs 中实现
    }
}