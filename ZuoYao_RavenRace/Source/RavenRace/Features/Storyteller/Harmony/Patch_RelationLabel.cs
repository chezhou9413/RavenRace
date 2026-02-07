using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace RavenRace.Features.Storyteller.Harmony
{
    // [修复] 拦截关系名称显示，使其显示自定义称呼
    [HarmonyPatch(typeof(PawnRelationDef), "GetGenderSpecificLabel")]
    public static class Patch_RelationLabel
    {
        [HarmonyPostfix]
        public static void Postfix(PawnRelationDef __instance, Pawn pawn, ref string __result)
        {
            // 这个方法只传入了一个 pawn (关系的主体)，但我们需要知道“相对于谁”。
            // 原版 GetGenderSpecificLabel 并不包含 otherPawn 信息。
            // 这是一个难题。通常 UI 调用它是为了显示 "pawn 是 otherPawn 的 [Label]"。

            // 幸好，SocialCardUtility 在绘制时会调用 SocialCardUtility.GetRelationsString
            // 我们之前已经尝试 Patch 那个方法，但可能没生效或者逻辑有漏洞。
            // 让我们重新审视 Patch_SocialCardUtility_GetRelationsString。
        }
    }
}