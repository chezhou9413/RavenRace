using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;

namespace RavenRace.Features.UniqueWeapons.SpiritBeads
{
    /// <summary>
    /// 补丁：将灵卵拉珠的操作按钮注入到持有者的 Gizmo 列表中。
    /// 原版装备系统不会自动调用自定义 Comp 的 CompGetGizmosExtra。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_SpiritBeads_Gizmos
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            // 1. 先返回原有的 Gizmo
            foreach (var gizmo in values)
            {
                yield return gizmo;
            }

            // 2. 检查是否是玩家控制的殖民者
            if (__instance == null || !__instance.IsColonistPlayerControlled) yield break;

            // 3. 检查主武器是否是灵卵拉珠
            // 使用 EquipmentTracker 访问主武器
            if (__instance.equipment?.Primary == null) yield break;

            // 检查 DefName (字符串比较虽然稍微慢点，但最安全且不需要 DefOf 依赖)
            if (__instance.equipment.Primary.def.defName != "Raven_Weapon_SpiritBeads") yield break;

            // 4. 获取组件并生成按钮
            var comp = __instance.equipment.Primary.GetComp<CompSpiritBeads>();
            if (comp != null)
            {
                foreach (var g in comp.GetEquippedGizmos(__instance))
                {
                    yield return g;
                }
            }
        }
    }
}