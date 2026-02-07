using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.DefenseSystem.Concealment
{
    // 1. 隐身补丁：拦截 Building_Turret.ThreatDisabled
    [HarmonyPatch(typeof(Building_Turret), "ThreatDisabled")]
    public static class Patch_ThreatDisabled
    {
        [HarmonyPostfix]
        public static void Postfix(Building_Turret __instance, ref bool __result)
        {
            if (__result) return;

            if (__instance is Building_Concealment concealment)
            {
                // 只要没暴露，就是隐身的 (AI 不会主动攻击它)
                if (!concealment.IsRevealed)
                {
                    __result = true;
                }
            }
        }
    }

    // 2. 开火检测补丁：拦截 Building_TurretGun.TryStartShootSomething
    [HarmonyPatch(typeof(Building_TurretGun), "TryStartShootSomething")]
    public static class Patch_TryStartShootSomething
    {
        // 暴露距离阈值 (15格)
        private const float RevealDistanceThreshold = 1f;

        [HarmonyPrefix]
        public static bool Prefix(Building_TurretGun __instance, bool canBeginBurstImmediately)
        {
            if (__instance is Building_Concealment concealment)
            {
                // 1. 如果里面没人，绝对禁止开火
                if (!concealment.HasOccupant) return false;

                // 2. 检测开火与暴露逻辑
                if (concealment.CurrentTarget.IsValid)
                {
                    // 获取冷却状态
                    var field = AccessTools.Field(typeof(Building_TurretGun), "burstCooldownTicksLeft");
                    int cooldown = (int)field.GetValue(__instance);

                    // 只有在真的准备开火时才判定 (冷却完毕)
                    if (cooldown <= 0)
                    {
                        bool shouldReveal = false;
                        float dist = concealment.Position.DistanceTo(concealment.CurrentTarget.Cell);

                        // [核心修改] 暴露条件：
                        // A. 距离 < 阈值 (近距离)
                        // B. 目标是敌对生物 (Pawn)
                        if (dist < RevealDistanceThreshold)
                        {
                            if (concealment.CurrentTarget.Thing is Pawn targetPawn &&
                                targetPawn.HostileTo(concealment.Faction))
                            {
                                shouldReveal = true;
                            }
                        }

                        if (shouldReveal)
                        {
                            concealment.lastAttackTick = Find.TickManager.TicksGame;

                            // 如果是从隐蔽转为暴露，提示一下
                            if (!concealment.IsRevealed)
                            {
                                MoteMaker.ThrowText(concealment.DrawPos, concealment.Map, "已暴露!", Color.red);
                            }
                        }
                    }
                }
            }
            return true; // 放行原版逻辑
        }
    }
}