using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace RavenRace.Features.Storyteller.Harmony
{
    // [核心] 拦截好感度计算，强制返回锁定的值
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "OpinionOf")]
    public static class Patch_OpinionLock
    {
        // 修复 CS1061: 使用 Harmony 注入私有字段 ___pawn
        [HarmonyPrefix]
        public static bool Prefix(Pawn_RelationsTracker __instance, Pawn other, ref int __result, Pawn ___pawn)
        {
            // 通过 Harmony 注入获取私有字段 pawn
            Pawn subject = ___pawn;

            // 安全检查
            if (subject == null || other == null) return true;

            // 1. 检查是否存在别天神的特殊关系
            // 这是一个双向检查：只要两人之间有这种绝对关系，就触发锁定逻辑
            bool isServantToMaster = subject.relations.DirectRelationExists(RavenDefOf.Raven_Relation_AbsoluteMaster, other);
            bool isMasterToServant = subject.relations.DirectRelationExists(RavenDefOf.Raven_Relation_LoyalServant, other);

            if (isServantToMaster || isMasterToServant)
            {
                // 2. 从 WorldComponent 获取我们在 Dialog 中设定的自定义数值
                var tracker = Find.World.GetComponent<WorldComponent_RavenRelationTracker>();
                if (tracker != null)
                {
                    // GetLockedOpinion 内部已经处理了 Key 的生成 (SubjectID_OtherID)
                    int? lockedVal = tracker.GetLockedOpinion(subject, other);

                    if (lockedVal.HasValue)
                    {
                        // 3. 强制覆盖结果并拦截原版计算
                        // 这意味着原版的所有加减分逻辑都不会执行，直接返回这个锁定的值
                        __result = lockedVal.Value;
                        return false;
                    }
                }
            }

            // 如果没有特殊关系，或者没有设定值，执行原版逻辑
            return true;
        }
    }
}