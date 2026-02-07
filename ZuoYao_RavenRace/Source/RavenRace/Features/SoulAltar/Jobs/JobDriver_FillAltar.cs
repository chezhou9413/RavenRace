using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace
{
    public class JobDriver_FillAltar : JobDriver
    {
        private const TargetIndex ItemInd = TargetIndex.A;
        private const TargetIndex BuildingInd = TargetIndex.B;

        protected Thing Item => job.GetTarget(ItemInd).Thing;
        protected Building_AltarInfuser Infuser => (Building_AltarInfuser)job.GetTarget(BuildingInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 预定物品和建筑
            return pawn.Reserve(Item, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(Infuser, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. 验证
            this.FailOnDestroyedOrNull(ItemInd);
            this.FailOnDestroyedOrNull(BuildingInd);
            // 如果建筑里已经有东西了，或者目标改变了，失败
            this.FailOn(() => Infuser.innerContainer.Count > 0 || Infuser.targetDef != Item.def);

            // 2. 捡起物品
            yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(ItemInd);

            yield return Toils_Haul.StartCarryThing(ItemInd, false, false, false);

            // 3. 运送到建筑
            yield return Toils_Goto.GotoThing(BuildingInd, PathEndMode.Touch);

            // 4. 放入
            Toil deposit = ToilMaker.MakeToil("Deposit");
            deposit.initAction = delegate
            {
                if (pawn.carryTracker.CarriedThing == null) return;

                // 尝试放入
                if (Infuser.TryAcceptItem(pawn.carryTracker.CarriedThing))
                {
                    // 成功放入，无需额外操作，TryAcceptItem 会处理 ThingOwner
                }
            };
            deposit.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return deposit;
        }
    }
}