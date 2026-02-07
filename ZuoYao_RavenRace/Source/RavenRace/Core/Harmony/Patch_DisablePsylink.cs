using HarmonyLib;
using Verse;
using RimWorld;

namespace RavenRace.Core.Harmony
{
    /// <summary>
    /// 核心功能补丁：禁用由非灵能技能（如强制求爱）错误触发的灵能熵UI。
    /// 这是一个全局补丁，放置在Core文件夹下非常合适，用于解决根本性问题。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_PsychicEntropyTracker), "IsPsychicallySensitive", MethodType.Getter)]
    public static class Patch_DisablePsylink
    {
        /// <summary>
        /// 在原版 IsPsychicallySensitive 属性的get方法执行后运行。
        /// </summary>
        /// <param name="__instance">Pawn_PsychicEntropyTracker的实例。</param>
        /// <param name="pawn">该组件所属的Pawn。</param>
        /// <param name="__result">原版方法的返回值，我们可以修改它。</param>
        [HarmonyPostfix]
        public static void Postfix(Pawn_PsychicEntropyTracker __instance, Pawn ___pawn, ref bool __result)
        {
            // 如果原版判断结果已经是“不敏感”，则无需任何操作。
            if (!__result) return;

            Pawn p = ___pawn;
            if (p == null) return;

            // 检查该Pawn是否拥有真正的灵能等级（来自帝国、启灵树或心灵武器）。
            bool hasRealPsylink = p.health.hediffSet.HasHediff(HediffDefOf.PsychicAmplifier);

            // 如果该Pawn没有真正的灵能等级，但游戏却因为他有技能（如我们的强制求爱）而误判为“灵能敏感”，
            // 我们就强制将结果改回false，从而隐藏错误的灵能熵UI。
            if (!hasRealPsylink && __instance.EntropyValue <= float.Epsilon)
            {
                __result = false;
            }
        }
    }
}