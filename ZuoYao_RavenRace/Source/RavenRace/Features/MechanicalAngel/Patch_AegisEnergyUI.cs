using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.MechanicalAngel
{
    /// <summary>
    /// 伪装补丁：劫持 Need_MechEnergy，将艾吉斯的“机械能”UI伪装成“淫能”。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_AegisEnergyUI
    {
        // 我们需要通过反射获取私有字段 pawn
        private static Pawn GetPawn(Need __instance)
        {
            return Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
        }

        private static bool IsAegis(Pawn pawn)
        {
            return pawn != null && pawn.def.defName == "Raven_Mech_Aegis";
        }

        /// <summary>
        /// 劫持 1：替换标题文本 (LabelCap)
        /// </summary>
        [HarmonyPatch(typeof(Need), "get_LabelCap")]
        [HarmonyPostfix]
        public static void Postfix_LabelCap(Need __instance, ref string __result)
        {
            if (__instance is Need_MechEnergy mechEnergy)
            {
                Pawn pawn = GetPawn(mechEnergy);
                if (IsAegis(pawn))
                {
                    // 使用富文本标签染成粉色
                    __result = "<color=#FF69B4>淫能</color>";
                }
            }
        }

        /// <summary>
        /// 劫持 2：替换鼠标悬停的 Tooltip 提示文本
        /// </summary>
        [HarmonyPatch(typeof(Need_MechEnergy), "GetTipString")]
        [HarmonyPostfix]
        public static void Postfix_GetTipString(Need_MechEnergy __instance, ref string __result)
        {
            Pawn pawn = GetPawn(__instance);
            if (IsAegis(pawn))
            {
                // 重构悬停提示信息
                string newTip = "<color=#FF69B4>淫能: " + __instance.CurLevelPercentage.ToStringPercent() + "</color>\n" +
                                "艾吉斯核心特有的能量系统。必须通过与生命体发生剧烈互动来汲取精气充能。\n\n" +
                                "当前每天自然流失: " + (__instance.FallPerDay / 100f).ToStringPercent();
                __result = newTip;
            }
        }

        /// <summary>
        /// 劫持 3：在绘制能量条前，如果是艾吉斯，强制将 GUI.color 染成粉色。
        /// 绘制结束后恢复原样。这样不需要修改复杂的绘制底层。
        /// </summary>
        [HarmonyPatch(typeof(Need), "DrawOnGUI")]
        [HarmonyPrefix]
        public static void Prefix_DrawOnGUI(Need __instance, out Color __state)
        {
            __state = GUI.color; // 记录原有颜色
            if (__instance is Need_MechEnergy mechEnergy)
            {
                Pawn pawn = GetPawn(mechEnergy);
                if (IsAegis(pawn))
                {
                    // 能量条染成粉色！
                    GUI.color = new Color(1f, 0.41f, 0.7f, 1f);
                }
            }
        }

        [HarmonyPatch(typeof(Need), "DrawOnGUI")]
        [HarmonyPostfix]
        public static void Postfix_DrawOnGUI(Need __instance, Color __state)
        {
            // 恢复 UI 颜色，防止污染其他界面
            GUI.color = __state;
        }
    }
}