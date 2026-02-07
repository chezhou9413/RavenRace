using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RavenRace
{
    // [修复] 拦截社交面板上的关系文本显示
    [HarmonyPatch(typeof(SocialCardUtility), "GetRelationsString")]
    public static class Patch_SocialCardUtility_GetRelationsString
    {
        [HarmonyPrefix]
        public static bool Prefix(object entry, Pawn selPawnForSocialInfo, ref string __result)
        {
            if (entry == null || selPawnForSocialInfo == null) return true;

            // 反射获取 otherPawn
            var fieldOtherPawn = AccessTools.Field(entry.GetType(), "otherPawn");
            Pawn otherPawn = (Pawn)fieldOtherPawn.GetValue(entry);

            if (otherPawn == null) return true;

            var tracker = Find.World.GetComponent<WorldComponent_RavenRelationTracker>();
            if (tracker == null) return true;

            // 1. selPawn 看 otherPawn (selPawn 是观察者)
            // 检查关系：otherPawn 是 selPawn 的什么人？

            // 如果 selPawn 是主人，otherPawn 是奴仆 -> 显示 "忠诚奴仆" (自定义)
            if (selPawnForSocialInfo.relations.DirectRelationExists(RavenDefOf.Raven_Relation_LoyalServant, otherPawn))
            {
                // 获取 "Servant" 的自定义称呼
                string label = tracker.GetServantLabel(selPawnForSocialInfo, otherPawn);
                if (!string.IsNullOrEmpty(label))
                {
                    // 还要附带其他关系吗？原版会拼接 "Friend, Lover"。
                    // 如果我们想独占，直接返回 label。
                    // 考虑到 "绝对" 控制，我们只显示这个。
                    __result = label;
                    return false;
                }
            }

            // 如果 selPawn 是奴仆，otherPawn 是主人 -> 显示 "绝对主人" (自定义)
            if (selPawnForSocialInfo.relations.DirectRelationExists(RavenDefOf.Raven_Relation_AbsoluteMaster, otherPawn))
            {
                string label = tracker.GetMasterLabel(otherPawn, selPawnForSocialInfo);
                if (!string.IsNullOrEmpty(label))
                {
                    __result = label;
                    return false;
                }
            }

            return true;
        }
    }
}