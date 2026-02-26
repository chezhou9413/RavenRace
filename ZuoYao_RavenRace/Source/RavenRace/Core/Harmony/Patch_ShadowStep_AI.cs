using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RavenRace.Features.UniqueEquipment.ShadowCloak;

namespace RavenRace.Core.Harmony
{
    /// <summary>
    /// 全局AI补丁，用于实现暗影斗篷的“影步”效果。
    /// 当Pawn拥有影步状态时，此补丁会阻止AI战斗逻辑为其分配攻击任务，
    /// 从而让Pawn能够利用加速效果脱离战场，而不是原地反击。
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class Patch_ShadowStep_AI
    {
        /// <summary>
        /// 在AI尝试给予战斗任务前执行的前缀补丁。
        /// </summary>
        /// <param name="pawn">执行AI逻辑的Pawn。</param>
        /// <param name="__result">存储将要返回的Job的引用。</param>
        /// <returns>返回false以阻止原版方法执行。</returns>
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            // 检查Pawn是否拥有“影步”或“暗影冷却”的Hediff
            if (pawn.health != null &&
                (pawn.health.hediffSet.HasHediff(ShadowCloakDefOf.Raven_Hediff_ShadowStep) ||
                 pawn.health.hediffSet.HasHediff(ShadowCloakDefOf.Raven_Hediff_ShadowAttackCooldown)))
            {
                // 如果有，则不分配任何战斗任务（返回null），让Pawn可以自由移动
                __result = null;
                // 拦截原版索敌逻辑，阻止其覆盖我们的决定
                return false;
            }
            // 如果没有相关状态，则正常执行AI的战斗索敌逻辑
            return true;
        }
    }
}