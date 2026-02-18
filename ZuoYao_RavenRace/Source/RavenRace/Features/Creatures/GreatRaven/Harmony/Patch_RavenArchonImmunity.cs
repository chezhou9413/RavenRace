using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Core.Harmony
{
    /// <summary>
    /// 渡鸦大统领：绝对精神免疫补丁 (保留自卫本能)
    /// </summary>
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public static class Patch_RavenArchonImmunity
    {
        [HarmonyPrefix]
        public static bool Prefix(MentalStateHandler __instance, Pawn ___pawn, MentalStateDef stateDef, bool causedByDamage, ref bool __result)
        {
            // 1. 检查是否为渡鸦大统领
            if (___pawn == null || ___pawn.def.defName != "Raven_HighArchon")
            {
                return true; // 对其他生物不生效
            }

            // 2. 允许列表

            // 情况 A: 猎杀人类 (Manhunter) 且 是由伤害引起的 (causedByDamage = true)
            // 这代表野生动物被攻击后的反击
            if (stateDef == MentalStateDefOf.Manhunter && causedByDamage)
            {
                return true; // 允许进入状态
            }

            // 情况 B: 社交争斗 (可选，这里暂时允许，以免看着像木头)
            if (stateDef == MentalStateDefOf.SocialFighting)
            {
                return true;
            }

            // 3. 拦截列表 (除此之外的一切)
            // 包括：狂暴(Berserk)、各种因心情导致的崩溃、事件触发的Manhunter脉冲(通常causedByDamage=false)、以及PanicFlee

            // 调试日志 (可选)
            RavenModUtility.LogVerbose($"拦截了大统领 {___pawn.LabelShort} 的精神状态: {stateDef.defName}, CausedByDamage: {causedByDamage}");

            __result = false;
            return false; // 拦截执行
        }
    }
}