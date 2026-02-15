using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace RavenRace.Features.CustomPawn.Binah
{
    /// <summary>
    /// Binah 生成补丁：
    /// 当 Binah 生成或加载时，强制检查并赋予其专属技能。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_BinahSkills
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || __instance.Destroyed) return;

            // 检查是否为 Binah (通过 PawnKind 判断)
            // 务必确保 PawnKindDefName 是正确的
            if (__instance.kindDef != null && __instance.kindDef.defName == "Raven_PawnKind_Binah")
            {
                EnsureBinahAbilities(__instance);
            }
        }

        private static void EnsureBinahAbilities(Pawn p)
        {
            if (p.abilities == null) return;

            // 定义 Binah 的专属技能列表
            List<AbilityDef> binahAbilities = new List<AbilityDef>
            {
                BinahDefOf.Raven_Ability_Binah_PillarShot,
                BinahDefOf.Raven_Ability_Binah_Shockwave,
                BinahDefOf.Raven_Ability_Binah_DegradationPillar,
                BinahDefOf.Raven_Ability_Binah_DegradationLock
            };

            foreach (var abilityDef in binahAbilities)
            {
                if (abilityDef != null && p.abilities.GetAbility(abilityDef) == null)
                {
                    p.abilities.GainAbility(abilityDef);
                }
            }
        }
    }
}