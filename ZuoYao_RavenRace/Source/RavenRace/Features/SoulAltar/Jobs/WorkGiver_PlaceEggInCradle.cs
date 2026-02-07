using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace
{
    public class WorkGiver_PlaceEggInCradle : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(RavenDefOf.Raven_SpiritEgg);
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        // 覆盖 HasJobOnThing 确保右键菜单正确显示预留状态
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // 1. 基础检查
            if (t.def != RavenDefOf.Raven_SpiritEgg) return false;

            // 2. 已经在容器里了吗？
            if (t.holdingOwner != null && t.holdingOwner.Owner is Building_Cradle) return false;

            // 3. 物品是否被禁止或不可达？
            if (t.IsForbidden(pawn) || !pawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Deadly)) return false;

            // 4. 物品预留检查 (这里失败会显示 "Reserved by X")
            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;

            // 5. 是否有可用的摇篮？
            Building_Cradle cradle = FindBestCradle(pawn, t, forced);
            if (cradle == null)
            {
                // 如果是强制工作（右键），这里虽然返回false，但因为上面通过了Reserve检查，
                // RimWorld可能会提示 "No usable cradle" 之类的，或者静默失败。
                // 若要显示特定原因，比较麻烦，通常这就够了。
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_Cradle cradle = FindBestCradle(pawn, t, forced);
            if (cradle == null) return null;

            Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_PlaceEggInCradle, t, cradle);
            job.count = 1;
            return job;
        }

        private Building_Cradle FindBestCradle(Pawn pawn, Thing egg, bool forced)
        {
            // 获取地图上所有属于玩家的 Building_Cradle
            var allCradles = pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Cradle>();

            if (allCradles == null || !allCradles.Any()) return null;

            // 筛选条件
            var validCradles = allCradles.Where(c =>
                !c.IsForbidden(pawn) &&
                c.allowAutoLoad && // 检查自动装填开关
                c.GetDirectlyHeldThings().Count == 0 && // 必须是空的
                pawn.CanReach(c, PathEndMode.Touch, Danger.Deadly) &&
                pawn.CanReserve(c, 1, -1, null, forced) // 必须能预留摇篮
            );

            // 分组：高级祭坛 vs 普通摇篮
            var highPriority = validCradles.Where(c => c.def.defName == "Raven_SoulAltar_Core");
            var lowPriority = validCradles.Where(c => c.def.defName != "Raven_SoulAltar_Core");

            // 优先找高级的，按距离排序
            var bestHigh = GenClosest.ClosestThing_Global_Reachable(
                egg.Position,
                pawn.Map,
                highPriority,
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                9999f
            ) as Building_Cradle;

            if (bestHigh != null) return bestHigh;

            // 其次找普通的
            var bestLow = GenClosest.ClosestThing_Global_Reachable(
                egg.Position,
                pawn.Map,
                lowPriority,
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                9999f
            ) as Building_Cradle;

            return bestLow;
        }
    }
}