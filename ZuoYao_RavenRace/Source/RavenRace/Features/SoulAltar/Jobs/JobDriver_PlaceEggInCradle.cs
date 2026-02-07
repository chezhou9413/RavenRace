using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace
{
    public class JobDriver_PlaceEggInCradle : JobDriver
    {
        private const TargetIndex EggInd = TargetIndex.A;
        private const TargetIndex CradleInd = TargetIndex.B;

        protected Thing Egg => job.GetTarget(EggInd).Thing;
        protected Building_Cradle Cradle => job.GetTarget(CradleInd).Thing as Building_Cradle;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 预定蛋
            if (!pawn.Reserve(Egg, job, 1, -1, null, errorOnFailed)) return false;
            // 预定摇篮
            if (!pawn.Reserve(Cradle, job, 1, 1, null, errorOnFailed)) return false;
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. 如果失败条件满足
            this.FailOnDestroyedOrNull(EggInd);
            this.FailOnDestroyedOrNull(CradleInd);
            this.FailOn(() => Cradle.GetDirectlyHeldThings().Count > 0); // 如果摇篮满了就失败

            // 2. 走向蛋
            yield return Toils_Goto.GotoThing(EggInd, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(EggInd);

            // 3. 拿起蛋
            yield return Toils_Haul.StartCarryThing(EggInd);

            // 4. 走向摇篮
            yield return Toils_Goto.GotoThing(CradleInd, PathEndMode.Touch);

            // 5. 放入摇篮
            Toil placeToil = ToilMaker.MakeToil("PlaceEgg");
            placeToil.initAction = delegate
            {
                if (pawn.carryTracker.CarriedThing == null) return;

                // 尝试将手中的东西放入摇篮
                if (Cradle.TryAcceptEgg(pawn.carryTracker.CarriedThing))
                {
                    // 成功放入，携带物会被转移，这里不需要手动 Destroy
                    pawn.carryTracker.innerContainer.ClearAndDestroyContents(); // 双重保险，通常 TryAdd 会处理移除
                }
            };
            placeToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return placeToil;
        }
    }
}