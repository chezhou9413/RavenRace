using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.Servitude.Harmony
{
    /// <summary>
    /// 提供主奴关系可视化的底层渲染补丁。
    /// 选中处于侍奉关系的 Pawn 时，会在其与主人/奴隶之间画一条粉色连线。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "DrawExtraSelectionOverlays")]
    public static class Patch_Pawn_DrawExtraSelectionOverlays
    {
        // 创建一个静态材质缓存（粉色，略带半透明）
        private static readonly Material ConnectionLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.4f, 0.8f, 0.8f));

        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || !__instance.Spawned) return;

            var manager = ServitudeManager.Get();
            if (manager == null) return;

            // 1. 如果选中的是主人，画线指向所有的奴隶
            if (manager.IsMaster(__instance))
            {
                foreach (var servant in manager.GetServants(__instance))
                {
                    // 确保奴隶存活且在同一张地图上
                    if (servant != null && servant.Spawned && servant.Map == __instance.Map)
                    {
                        GenDraw.DrawLineBetween(__instance.TrueCenter(), servant.TrueCenter(), ConnectionLineMat, 0.2f);
                    }
                }
            }
            // 2. 如果选中的是奴隶，画线指向他的主人
            else if (manager.IsServant(__instance))
            {
                var master = manager.GetMaster(__instance);
                if (master != null && master.Spawned && master.Map == __instance.Map)
                {
                    GenDraw.DrawLineBetween(__instance.TrueCenter(), master.TrueCenter(), ConnectionLineMat, 0.2f);
                }
            }
        }
    }
}