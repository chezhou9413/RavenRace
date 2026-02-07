using HarmonyLib;
using RimWorld;
using Verse;

namespace RavenRace.Features.BedSharing.Harmony
{
    // 1. 解除 "只有情侣才能共用床" 的限制 (分配逻辑)
    [HarmonyPatch(typeof(RestUtility), "BedOwnerWillShare")]
    public static class Patch_RestUtility_BedOwnerWillShare
    {
        [HarmonyPostfix]
        public static void Postfix(Building_Bed bed, Pawn sleeper, ref bool __result)
        {
            if (__result) return; // 原版已允许

            if (bed == null) return;

            bool sleeperIsRaven = RavenBedSharingUtility.IsRaven(sleeper);
            bool ownerHasRaven = false;

            foreach (Pawn owner in bed.OwnersForReading)
            {
                if (RavenBedSharingUtility.IsRaven(owner))
                {
                    ownerHasRaven = true;
                    break;
                }
            }

            // 只要有一方是渡鸦，就允许分配，无视关系
            if (sleeperIsRaven || ownerHasRaven)
            {
                __result = true;
            }
        }
    }

    // 2. 解除 Ideology (文化DLC) 的戒律限制 (UI 显示禁止图标 & 逻辑阻断)
    // 关键修正：必须 Patch CompAssignableToPawn_Bed 而不是基类 CompAssignableToPawn
    // 因为床的组件重写了这个方法来检查 BedUtility.WillingToShareBed
    [HarmonyPatch(typeof(CompAssignableToPawn_Bed), "IdeoligionForbids")]
    public static class Patch_CompAssignableToPawn_Bed_IdeoligionForbids
    {
        [HarmonyPostfix]
        public static void Postfix(CompAssignableToPawn_Bed __instance, Pawn pawn, ref bool __result)
        {
            if (!__result) return; // 如果本来就不禁止，无需干涉

            if (!(__instance.parent is Building_Bed bed)) return;

            // 逻辑：如果分配对象是渡鸦，或者床主里有渡鸦，则无视戒律
            bool sleeperIsRaven = RavenBedSharingUtility.IsRaven(pawn);
            bool ownerHasRaven = false;

            foreach (Pawn owner in bed.OwnersForReading)
            {
                if (RavenBedSharingUtility.IsRaven(owner))
                {
                    ownerHasRaven = true;
                    break;
                }
            }

            if (sleeperIsRaven || ownerHasRaven)
            {
                __result = false; // 强制返回 false (不禁止)
            }
        }
    }
}