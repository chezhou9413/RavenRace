// File: RJWCompat/Source/RJWCompat/Patch_ForceLovin.cs
using HarmonyLib;
using RimWorld;
using Verse;
using RavenRace.RJWCompat.UI; // 引入新的UI命名空间

namespace RavenRace.RJWCompat
{
    /// <summary>
    /// 补丁：拦截渡鸦族“强制求爱”技能的Apply方法。
    /// 作用：阻止原版技能效果的执行，转而打开我们自定义的RJW互动选择UI。
    /// </summary>
    [HarmonyPatch("RavenRace.CompAbilityEffect_ForceLovin", "Apply")]
    public static class Patch_ForceLovin_Apply
    {
        /// <summary>
        /// 在原方法执行前运行。
        /// </summary>
        /// <returns>返回 'false' 将完全阻止原方法的执行。</returns>
        [HarmonyPrefix]
        public static bool Prefix(LocalTargetInfo target, CompAbilityEffect __instance)
        {
            Log.Message($"[RavenRace RJWCompat] 'ForceLovin.Apply' Prefix patch triggered!");

            // 弹出全新的、带分类的UI窗口
            Find.WindowStack.Add(new Dialog_SelectRjwInteraction(__instance.parent.pawn, target.Pawn));

            // 返回 false，阻止原版的 ForceLovin Job 创建，将控制权完全交给我们的UI和RJW。
            return false;
        }
    }
}