using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.DefenseSystem.Concealment
{
    public class JobDriver_EnterConcealment : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() =>
            {
                var building = job.targetA.Thing as Building_Concealment;
                return building == null || building.HasOccupant;
            });

            // 1. 移动
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 2. 进入
            Toil enter = ToilMaker.MakeToil("Enter");
            enter.initAction = delegate
            {
                var building = job.targetA.Thing as Building_Concealment;
                if (building != null)
                {
                    // 先从地图消失
                    pawn.DeSpawn(DestroyMode.Vanish);

                    // 再尝试进入
                    if (!building.TryAcceptPawn(pawn))
                    {
                        // 失败回退：重新生成
                        GenSpawn.Spawn(pawn, building.InteractionCell, building.Map);
                    }
                }
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }
}