using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace RavenRace.Features.BedSharing.Harmony
{
    // 1. 消除 "不得不共用床铺" 的负面心情
    [HarmonyPatch(typeof(LovePartnerRelationUtility), "GetMostDislikedNonPartnerBedOwner")]
    public static class Patch_LovePartnerRelationUtility_GetMostDisliked
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn p, ref Pawn __result)
        {
            // 原版方法返回了那个 "令我不爽的非伴侣床友"
            if (__result != null)
            {
                // 如果那个床友是渡鸦，或者我自己是渡鸦
                // 渡鸦不会因为和别人睡而不爽，别人和渡鸦睡也不会不爽
                if (RavenBedSharingUtility.IsRaven(__result) || RavenBedSharingUtility.IsRaven(p))
                {
                    // 返回 null，告诉 ThoughtWorker "没有令我不爽的人"
                    __result = null;
                }
            }
        }
    }

    // 2. 在睡觉时添加正面心情和社交关系
    [HarmonyPatch(typeof(JobDriver_LayDown), "LayDownToil")]
    public static class Patch_JobDriver_LayDown_LayDownToil
    {
        [HarmonyPostfix]
        public static void Postfix(JobDriver_LayDown __instance, ref Toil __result)
        {
            // 获取生成的 Toil
            Toil layDownToil = __result;

            // 注入我们的逻辑：每 2500 tick (1小时) 检查一次
            layDownToil.AddPreTickAction(() =>
            {
                Pawn pawn = __instance.pawn;

                // 1. 频率检查
                if (!pawn.IsHashIntervalTick(2500)) return;

                // 2. 状态检查：必须在床上且已睡着 (或者是躺着休息)
                if (!pawn.Spawned || !pawn.InBed()) return;

                Building_Bed bed = pawn.CurrentBed();
                if (bed == null) return;

                // 3. 寻找床伴
                // 遍历床上的其他人
                List<Pawn> occupants = new List<Pawn>();
                for (int i = 0; i < bed.SleepingSlotsCount; i++)
                {
                    Pawn occupant = bed.GetCurOccupant(i);
                    if (occupant != null && occupant != pawn)
                    {
                        occupants.Add(occupant);
                    }
                }

                if (occupants.Count == 0) return;

                // 4. 对每个床伴执行逻辑
                foreach (Pawn partner in occupants)
                {
                    if (RavenBedSharingUtility.ShouldTriggerRavenSnuggle(pawn, partner))
                    {
                        RavenBedSharingUtility.DoSnuggleEffect(pawn, partner);
                    }
                }
            });
        }
    }
}