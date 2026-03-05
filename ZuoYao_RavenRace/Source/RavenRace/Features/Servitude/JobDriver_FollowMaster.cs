using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.Servitude
{
    public class JobDriver_FollowMaster : JobDriver
    {
        private const TargetIndex MasterInd = TargetIndex.A;
        private const float FollowRadius = 4f;

        protected Pawn Master => (Pawn)job.GetTarget(MasterInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(MasterInd);
            this.FailOn(() => Master.Map != pawn.Map);

            // 主循环Toil
            Toil follow = ToilMaker.MakeToil("Follow");
            follow.defaultCompleteMode = ToilCompleteMode.Never;
            follow.tickAction = delegate
            {
                // 如果离主人太远，就走过去
                if (!pawn.Position.InHorDistOf(Master.Position, FollowRadius))
                {
                    pawn.pather.StartPath(Master.Position, PathEndMode.OnCell);
                }
                // 如果已经很近了，就停下来等待
                else
                {
                    pawn.pather.StopDead();
                }
            };

            // 每隔一段时间就重新评估一下，看看有没有更重要的事做
            follow.defaultDuration = 250;
            follow.defaultCompleteMode = ToilCompleteMode.Delay;

            yield return follow;
        }
    }
}