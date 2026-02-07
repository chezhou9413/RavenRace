using HarmonyLib;
using RimWorld;
using Verse;
using System.Text;

namespace RavenRace.Features.Storyteller.Harmony
{
    /// <summary>
    /// 拦截社交面板的好感度详细解释，将别天神关系的显示替换为自定义内容。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "OpinionExplanation")]
    public static class Patch_OpinionExplanation
    {
        // 使用 Postfix，在原版方法生成完字符串后进行修改
        [HarmonyPostfix]
        public static void Postfix(Pawn_RelationsTracker __instance, Pawn other, ref string __result, Pawn ___pawn)
        {
            Pawn subject = ___pawn;
            if (subject == null || other == null) return;

            var tracker = Find.World.GetComponent<WorldComponent_RavenRelationTracker>();
            if (tracker == null) return;

            // 检查是否存在我们的特殊关系
            if (subject.relations.DirectRelationExists(RavenDefOf.Raven_Relation_AbsoluteMaster, other) ||
                subject.relations.DirectRelationExists(RavenDefOf.Raven_Relation_LoyalServant, other))
            {
                // 1. 获取自定义数据
                string customLabel = tracker.GetMasterLabel(other, subject) ?? tracker.GetServantLabel(subject, other) ?? "关系错误";
                int? lockedOpinion = tracker.GetLockedOpinion(subject, other);

                if (lockedOpinion.HasValue)
                {
                    // 2. 找到原版生成的错误行
                    // 原版会根据关系Def生成一行，我们需要找到它
                    PawnRelationDef relationDef = subject.relations.GetDirectRelation(RavenDefOf.Raven_Relation_AbsoluteMaster, other)?.def ??
                                                  subject.relations.GetDirectRelation(RavenDefOf.Raven_Relation_LoyalServant, other)?.def;

                    if (relationDef != null)
                    {
                        // 构造旧的、错误的文本行，例如 " - 绝对主人: +0"
                        string oldLine = " - " + relationDef.GetGenderSpecificLabelCap(other) + ": " + relationDef.opinionOffset.ToStringWithSign();

                        // 构造新的、正确的文本行，例如 " - 我亲爱的主人: +100"
                        string newLine = " - " + customLabel + ": " + lockedOpinion.Value.ToStringWithSign();

                        // 3. 替换
                        // 使用 StringBuilder 以获得更好的性能和可读性
                        StringBuilder sb = new StringBuilder(__result);
                        sb.Replace(oldLine, newLine);
                        __result = sb.ToString();
                    }
                }
            }
        }
    }
}