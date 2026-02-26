using HarmonyLib;
using Verse;

namespace RavenRace.Core.Harmony
{
    /// <summary>
    /// 渡鸦族Pawn图形初始化修复补丁。
    /// 这是一个非常重要的全局补丁，用于解决Humanoid Alien Races (HAR)框架的一个常见问题：
    /// 当一个外星人Pawn在游戏运行时（非读档时）被动态生成时，其复杂的身体部件（BodyAddon）可能因为加载时机问题而渲染不正确或直接报错（显示为粉色方块或红字）。
    /// 此补丁通过在Pawn生成到地图上后，强制刷新其所有图形缓存，来确保所有自定义贴图都被正确加载和应用。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class PawnGraphicsPatch
    {
        /// <summary>
        /// 在原版的Pawn.SpawnSetup方法执行后运行的后缀补丁。
        /// </summary>
        /// <param name="__instance">被生成到地图上的Pawn实例。</param>
        /// <param name="respawningAfterLoad">如果Pawn是从存档中加载的，则为true。</param>
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, bool respawningAfterLoad)
        {
            // 我们只关心新生成的Pawn，从存档加载的Pawn图形通常是正确的。
            if (!respawningAfterLoad && __instance != null && __instance.def == RavenDefOf.Raven_Race)
            {
                // 安全检查，确保Drawer和renderer存在
                if (__instance.Drawer?.renderer != null)
                {
                    // 核心操作：调用SetAllGraphicsDirty()。
                    // 这会告诉渲染器：“这个Pawn的外观可能变了，请丢弃所有旧的缓存贴图，在下一帧重新计算并加载所有身体、头部、附加部件的贴图。”
                    __instance.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
        }
    }
}