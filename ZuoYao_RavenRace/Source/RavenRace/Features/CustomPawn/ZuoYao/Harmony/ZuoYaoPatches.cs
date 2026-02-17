using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace RavenRace.Features.CustomPawn.ZuoYao.Harmony
{
    /// <summary>
    /// 左爻专用功能补丁集合：仅包含“别天神”相关的关系显示和好感度锁定逻辑。
    /// 外观逻辑已移交 ChezhouLib 处理。
    /// </summary>
    [HarmonyPatch]
    public static class ZuoYaoPatches
    {
        // -----------------------------------------------------------
        // 1. 好感度锁定 (OpinionOf)
        // 如果存在别天神关系，强制返回 WorldComponent 中的数值
        // -----------------------------------------------------------
        [HarmonyPatch(typeof(Pawn_RelationsTracker), "OpinionOf")]
        [HarmonyPrefix]
        public static bool LockOpinion(Pawn_RelationsTracker __instance, Pawn other, ref int __result, Pawn ___pawn)
        {
            Pawn subject = ___pawn;
            if (subject == null || other == null) return true;

            // 检查关系是否存在 (双向)
            // ZuoYaoDefOf.Raven_Relation_AbsoluteMaster 等同于旧代码中的 RavenDefOf...
            bool hasRelation = subject.relations.DirectRelationExists(ZuoYaoDefOf.Raven_Relation_AbsoluteMaster, other) ||
                               subject.relations.DirectRelationExists(ZuoYaoDefOf.Raven_Relation_LoyalServant, other);

            if (hasRelation)
            {
                var tracker = Find.World.GetComponent<WorldComponent_RavenRelationTracker>();
                if (tracker != null)
                {
                    int? lockedVal = tracker.GetLockedOpinion(subject, other);
                    if (lockedVal.HasValue)
                    {
                        __result = lockedVal.Value;
                        return false; // 拦截原版逻辑，直接返回锁定值
                    }
                }
            }
            return true;
        }

        // -----------------------------------------------------------
        // 2. 关系文本替换 (SocialCardUtility.GetRelationsString)
        // 在社交面板列表显示自定义称呼（如 "绝对主人"）而非 "Friend"
        // -----------------------------------------------------------
        [HarmonyPatch(typeof(SocialCardUtility), "GetRelationsString")]
        [HarmonyPrefix]
        public static bool ReplaceRelationLabel(object entry, Pawn selPawnForSocialInfo, ref string __result)
        {
            if (entry == null || selPawnForSocialInfo == null) return true;

            // 反射获取 otherPawn (entry 是私有内部类)
            var fieldOtherPawn = AccessTools.Field(entry.GetType(), "otherPawn");
            Pawn otherPawn = (Pawn)fieldOtherPawn.GetValue(entry);

            if (otherPawn == null) return true;

            var tracker = Find.World.GetComponent<WorldComponent_RavenRelationTracker>();
            if (tracker == null) return true;

            // 获取自定义称呼 (Observer = selPawn, Target = otherPawn)
            string label = tracker.GetCustomLabel(selPawnForSocialInfo, otherPawn);

            if (!string.IsNullOrEmpty(label))
            {
                __result = label;
                return false; // 拦截原版，直接显示自定义称呼
            }

            return true;
        }

        // -----------------------------------------------------------
        // 3. 详细解释替换 (OpinionExplanation)
        // 在鼠标悬停的 Tooltip 中显示自定义文本和数值
        // -----------------------------------------------------------
        [HarmonyPatch(typeof(Pawn_RelationsTracker), "OpinionExplanation")]
        [HarmonyPostfix]
        public static void ReplaceOpinionExplanation(Pawn_RelationsTracker __instance, Pawn other, ref string __result, Pawn ___pawn)
        {
            Pawn subject = ___pawn;
            if (subject == null || other == null) return;

            var tracker = Find.World.GetComponent<WorldComponent_RavenRelationTracker>();
            if (tracker == null) return;

            // 检查是否存在我们的特殊关系 Def
            PawnRelationDef relationDef = subject.relations.GetDirectRelation(ZuoYaoDefOf.Raven_Relation_AbsoluteMaster, other)?.def ??
                                          subject.relations.GetDirectRelation(ZuoYaoDefOf.Raven_Relation_LoyalServant, other)?.def;

            if (relationDef != null)
            {
                string customLabel = tracker.GetCustomLabel(subject, other);
                int? lockedOpinion = tracker.GetLockedOpinion(subject, other);

                if (!string.IsNullOrEmpty(customLabel) && lockedOpinion.HasValue)
                {
                    // 构造需要被替换的旧文本 (原版格式: " - RelationLabel: +0")
                    // 我们在 XML 里把 OpinionOffset 设为 0，所以这里原版生成的是 +0
                    string oldLine = " - " + relationDef.GetGenderSpecificLabelCap(other) + ": " + relationDef.opinionOffset.ToStringWithSign();

                    // 构造新文本 (使用我们锁定的数值)
                    string newLine = " - " + customLabel + ": " + lockedOpinion.Value.ToStringWithSign();

                    StringBuilder sb = new StringBuilder(__result);
                    sb.Replace(oldLine, newLine);
                    __result = sb.ToString();
                }
            }
        }
    }
}