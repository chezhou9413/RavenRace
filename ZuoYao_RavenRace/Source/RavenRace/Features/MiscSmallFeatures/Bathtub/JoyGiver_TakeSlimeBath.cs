using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.Bathtub
{
    /// <summary>
    /// AI 自动寻找浴缸的极简逻辑
    /// </summary>
    public class JoyGiver_TakeSlimeBath : JoyGiver_InteractBuilding
    {
        // 注意：原版的 CanDoDuringGathering 是 protected，由于没特殊需要，我们不重写它以避免权限冲突。

        public override float GetChance(Pawn pawn)
        {
            // 应用设置中的倍率
            return base.GetChance(pawn) * RavenRaceMod.Settings.bathtubJoyWeightMultiplier;
        }

        // 核心：彻底剥离 IsSociallyProper 检测，像马蹄铁一样随地可用
        protected override bool CanInteractWith(Pawn pawn, Thing t, bool inBed)
        {
            if (!pawn.CanReserve(t, this.def.jobDef.joyMaxParticipants, -1, null, false)) return false;
            if (t.IsForbidden(pawn)) return false;
            if (t.Fogged()) return false;
            // 移除了 t.IsSociallyProper(pawn)，不再管它是不是在别人的卧室里
            return true;
        }

        protected override Job TryGivePlayJob(Pawn pawn, Thing t)
        {
            Building_RavenBathtub bathtub = t as Building_RavenBathtub;
            if (bathtub == null) return null;

            // 寻找一个空闲的格子
            IntVec3 targetCell = IntVec3.Invalid;
            CellRect rect = bathtub.OccupiedRect();

            foreach (IntVec3 cell in rect)
            {
                bool occupied = false;
                List<Thing> things = cell.GetThingList(bathtub.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn p && p.CurJobDef == RavenDefOf.Raven_Job_TakeSlimeBath)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied && pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.Some))
                {
                    targetCell = cell;
                    break;
                }
            }

            if (!targetCell.IsValid) return null;

            return JobMaker.MakeJob(this.def.jobDef, bathtub, targetCell);
        }
    }
}