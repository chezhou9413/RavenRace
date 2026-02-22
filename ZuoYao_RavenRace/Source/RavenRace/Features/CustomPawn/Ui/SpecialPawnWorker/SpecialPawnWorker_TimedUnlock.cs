using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RavenRace.Features.CustomPawn.Ui.SpecialPawnWorker
{
    // 示例：基于时间的解锁
    public class SpecialPawnWorker_TimedUnlock : SpecialPawnWorkerBase
    {
        private int unlockAfterTicks = 10 * 60;
        private int startTick = -1;

        public override bool UnlockCondition()
        {
            if (startTick < 0)
                startTick = Find.TickManager.TicksGame;

            return Find.TickManager.TicksGame - startTick >= unlockAfterTicks;
        }

        public override void OnUnlocked()
        {
            string message = def.LabelCap + " 已解锁！";
            Messages.Message(message, MessageTypeDefOf.PositiveEvent, historical: false);
        }
    }
}
