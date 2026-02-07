using RimWorld;
using Verse;
using System.Linq;

namespace RavenRace.Features.DegradationCharm.Comps
{
    /// <summary>
    /// 为“堕落符咒”物品添加的组件属性。
    /// </summary>
    public class CompProperties_CharmProvider : CompProperties
    {
        public CompProperties_CharmProvider() => this.compClass = typeof(CompCharmProvider);
    }

    /// <summary>
    /// “堕落符咒”的核心组件。
    /// 【已修正】该组件的逻辑被证明是错误的。它现在被废弃，仅作为一个标记使用。
    /// 真正的“携带时出现Gizmo”逻辑由 Harmony/Patch_Pawn_GetGizmos.cs 实现。
    /// </summary>
    public class CompCharmProvider : ThingComp
    {
        // 这个类现在是空的，只作为物品的标记，让我们的GetGizmos补丁可以识别它。
        // 之前在这里的Tick逻辑被证明是不可靠且错误的，已被完全移除。
    }
}