using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.DefenseSystem.Harmony
{
    /// <summary>
    /// 战斗AI补丁：提高假人的仇恨值
    /// 已优化：引入缓存检查，极大降低空转开销。
    /// </summary>
    [HarmonyPatch(typeof(AttackTargetFinder), "GetShootingTargetScore")]
    public static class Patch_AttackTargetFinder_GetShootingTargetScore
    {
        [HarmonyPostfix]
        public static void Postfix(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb, ref float __result)
        {
            // 优化 1: 快速失败检查
            // 如果地图上根本没有假人，或者目标本身不是物体，直接跳过
            if (!RavenDecoyCache.HasActiveDecoys) return;

            Thing t = target as Thing;
            if (t == null) return;

            // 优化 2: 使用 ID 缓存查找 (O(1)) 替代 Def 比较
            if (RavenDecoyCache.IsRegisteredDecoy(t))
            {
                // 只有当搜索者是敌人时才增加仇恨（避免友军误伤或奇怪行为）
                if (t.HostileTo(searcher.Thing))
                {
                    // 增加 1000 分，这几乎能覆盖任何其他优先级
                    // 原版分数通常在 0-100 之间
                    __result += 1000f;
                }
            }
        }
    }
}