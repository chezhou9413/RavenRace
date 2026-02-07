using HarmonyLib;
using Verse;

namespace RavenRace.Harmony
{
    /// <summary>
    /// 渡鸦族Pawn图形初始化修复补丁
    /// 作用：在Pawn生成到地图上时，强制刷新其所有图形，解决中途加入游戏时可能出现的贴图加载失败（红字）问题。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class PawnGraphicsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, bool respawningAfterLoad)
        {
            // 在生成Pawn后执行 (非从存档加载时)
            if (!respawningAfterLoad && __instance != null && __instance.def == RavenDefOf.Raven_Race)
            {
                // 安全检查
                if (__instance.Drawer?.renderer != null)
                {
                    // 强制渲染器重新计算和加载该Pawn的所有贴图
                    __instance.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
        }
    }
}